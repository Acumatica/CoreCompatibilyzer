using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CoreCompatibilyzer.ApiData.Model;
using CoreCompatibilyzer.ApiData.Providers;
using CoreCompatibilyzer.ApiData.Storage;
using CoreCompatibilyzer.DotNetRuntimeVersion;
using CoreCompatibilyzer.Runner.Analysis.Helpers;
using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Runner.Output;
using CoreCompatibilyzer.Runner.Output.Data;
using CoreCompatibilyzer.StaticAnalysis;
using CoreCompatibilyzer.Utils.Common;
using CoreCompatibilyzer.Utils.Roslyn.Suppression;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using Serilog;

namespace CoreCompatibilyzer.Runner.Analysis
{
    internal sealed class SolutionCompatibilityAnalyzer
	{
		private readonly IOutputterFactory _outputterFactory;
		private readonly IProjectReportBuilder	_reportBuilder;

		private readonly ImmutableArray<DiagnosticAnalyzer> _diagnosticAnalyzers;
		private readonly IApiStorage _bannedApiStorage;
		private readonly IApiStorage _whiteListStorage;

		private bool IsBannedStorageInitAndNonEmpty => _bannedApiStorage.ApiKindsCount > 0;

		private bool IsWhiteListInitAndNonEmpty => _whiteListStorage.ApiKindsCount > 0;

		private SolutionCompatibilityAnalyzer(IApiStorage bannedApiStorage, IApiStorage whiteListStorage, ImmutableArray<DiagnosticAnalyzer> diagnosticAnalyzers,
											  IProjectReportBuilder? customReportBuilder = null, IOutputterFactory? customOutputFactory = null)
        {
            _bannedApiStorage	 = bannedApiStorage;
			_whiteListStorage	 = whiteListStorage;
			_diagnosticAnalyzers = diagnosticAnalyzers;
			_reportBuilder 		 = customReportBuilder ?? new ProjectReportBuilder();
			_outputterFactory 	 = customOutputFactory ?? new ReportOutputterFactory();
		}

		public static async Task<SolutionCompatibilityAnalyzer> CreateAnalyzer(CancellationToken cancellationToken, 
																			   IApiDataProvider? customBannedApiDataProvider = null)
		{
			var bannedApiTask = ApiStorage.BannedApi.GetStorageAsync(cancellationToken, customBannedApiDataProvider);
			var whiteListTask = ApiStorage.WhiteList.GetStorageAsync(cancellationToken, customBannedApiDataProvider);

			var bannedApiAndWhiteList = await Task.WhenAll(bannedApiTask, whiteListTask).ConfigureAwait(false);

			IApiStorage bannedApiStorage = bannedApiAndWhiteList[0];
			IApiStorage whiteListStorage = bannedApiAndWhiteList[1];
			var diagnosticAnalyzers		 = CollectAnalyzers();

			return new SolutionCompatibilityAnalyzer(bannedApiStorage, whiteListStorage, diagnosticAnalyzers);
		}

		private static ImmutableArray<DiagnosticAnalyzer> CollectAnalyzers()
		{
			var analyzersAssemblyPath = typeof(CoreCompatibilyzerAnalyzer).Assembly.Location;
			var analyzerReference = new AnalyzerFileReference(analyzersAssemblyPath, new AnalyzerAssemblyLoader());
			var analyzers = analyzerReference.GetAnalyzers(LanguageNames.CSharp);

			return analyzers;
		}

		public async Task<RunResult> AnalyseSolution(Solution solution, AppAnalysisContext analysisContext, CancellationToken cancellationToken)
		{
			RunResult solutionValidationResult = RunResult.Success;
			var projectsToValidate = analysisContext.CodeSource.GetProjectsForValidation(solution)
															   .OrderBy(p => p.Name);


			using (var reportOutputter = _outputterFactory.CreateOutputter(analysisContext))
			{
				var projectReports 	  	  = new List<ProjectReport>(capacity: solution.ProjectIds.Count);
				var allUsedNamespaces 	  = new HashSet<string>();
				var allUsedTypes 	  	  = new HashSet<string>();
				IEnumerable<Api> usedApis = Enumerable.Empty<Api>();

				foreach (Project project in projectsToValidate)
				{
					Log.Information("Started validation of the project \"{ProjectName}\".", project.Name);

					if (cancellationToken.IsCancellationRequested)
					{
						Log.Information("Finished validation of the project \"{ProjectName}\". Project valudation result: {Result}.",
										project.Name, RunResult.Cancelled);
						solutionValidationResult = solutionValidationResult.Combine(RunResult.Cancelled);
						return solutionValidationResult;
					}

					var (projectValidationResult, projectReport, projectAnalysisData) = 
						await AnalyzeProject(project, analysisContext, cancellationToken).ConfigureAwait(false);

					if (projectReport != null)
						projectReports.Add(projectReport);

					solutionValidationResult = solutionValidationResult.Combine(projectValidationResult);

					if (projectAnalysisData != null)
					{
						allUsedNamespaces.AddRange(projectAnalysisData.UsedNamespaces);
						allUsedTypes.AddRange(projectAnalysisData.UsedBannedTypes);

						usedApis = usedApis.Concat(projectAnalysisData.UsedDistinctApis);
					}

					Log.Information("Finished validation of the project \"{ProjectName}\". Project valudation result: {Result}.",
									project.Name, projectValidationResult);
				}

				if (projectReports.Count > 0)
				{
					var distinctApisCalculator = new UsedDistinctApisCalculator(analysisContext, allUsedNamespaces, allUsedTypes);
					IEnumerable<Api> allDistinctApis = distinctApisCalculator.GetAllUsedApis(usedApis);
					var codeSourceReport = CreateCodeSourceReport(analysisContext, projectReports, allDistinctApis);

					reportOutputter.OutputReport(codeSourceReport, analysisContext, cancellationToken);
				}
			}

			return solutionValidationResult;
		}

		private async Task<(RunResult validationResult, ProjectReport? Report, DiagnosticsWithBannedApis? AnalysisData)> AnalyzeProject(
																								Project project, AppAnalysisContext analysisContext,
																								CancellationToken cancellationToken)
		{
			Log.Debug("Obtaining Roslyn compilation data for the project \"{ProjectName}\".", project.Name);
			var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

			if (compilation == null)
			{
				Log.Error("Failed to obtain Roslyn compilation data for the project with name \"{ProjectName}\" and path \"{ProjectPath}\".",
						  project.Name, project.FilePath);
				return (RunResult.RunTimeError, Report: null, AnalysisData: null);
			}

			Log.Debug("Obtained Roslyn compilation data for the project \"{ProjectName}\" successfully.", project.Name);
			Log.Debug("Obtaining .Net runtime version targeted by the project \"{ProjectName}\".", project.Name);

			var dotNetVersion = DotNetVersionsStorage.Instance.GetDotNetRuntimeVersion(compilation);
			var versionValidationResult = ValidateProjectVersion(project, dotNetVersion, analysisContext.TargetRuntime);

			if (versionValidationResult.HasValue)
				return (versionValidationResult.Value, Report: null, AnalysisData: null);

			if (!IsBannedStorageInitAndNonEmpty)
				return (RunResult.Success, Report: null, AnalysisData: null);
			
			var projectAnalysisResult = await RunAnalyzersOnProjectAsync(compilation, analysisContext, project, cancellationToken)
												.ConfigureAwait(false);
			return projectAnalysisResult;
		}

		private RunResult? ValidateProjectVersion(Project project, DotNetRuntime? projectVersion, DotNetRuntime targetVersion)
		{
			if (projectVersion == null)
			{
				Log.Error("Failed to get the .Net runtime version targeted by the project with name \"{ProjectName}\" and path \"{ProjectPath}\".",
						  project.Name, project.FilePath);
				return RunResult.RunTimeError;
			}

			Log.Information("Project \"{ProjectName}\" targeted .Net runtime version is \"{ProjectDotNetVersion}\".", project.Name, projectVersion.Value);

			if (DotNetRunTimeComparer.Instance.Compare(projectVersion.Value, targetVersion) >= 0)
			{
				Log.Information("The .Net runtime version \"{ProjectDotNetVersion}\" of the project \"{ProjectName}\" " +
								"is greater or equals to the target .Net runtime version \"{TargetDotNetVersion}\".",
								projectVersion.Value, project.Name, targetVersion);
				return RunResult.Success;
			}

			return null;
		}

		private async Task<(RunResult validationResult, ProjectReport? Report, DiagnosticsWithBannedApis? AnalysisData)> RunAnalyzersOnProjectAsync(
																						Compilation compilation, AppAnalysisContext analysisContext, 
																						Project project, CancellationToken cancellation)
		{
			if (_diagnosticAnalyzers.IsDefaultOrEmpty)
				return (RunResult.Success, Report: null, AnalysisData: null);

			SuppressionManager.UseSuppression = !analysisContext.DisableSuppressionMechanism;
			var compilationAnalysisOptions = new CompilationWithAnalyzersOptions(options: null!, OnAnalyzerException,
																				 concurrentAnalysis: !Debugger.IsAttached, 
																				 logAnalyzerExecutionTime: false);
			CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(compilationAnalysisOptions, _diagnosticAnalyzers, cancellation);

			var diagnosticResults = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(cancellation).ConfigureAwait(false);
			Log.Error("{Project} - Total Errors Count: {ErrorCount}", project.Name, diagnosticResults.Length);

			if (diagnosticResults.IsDefaultOrEmpty)
				return (RunResult.Success, Report: null, AnalysisData: null);

			var diagnosticsWithApis		= new DiagnosticsWithBannedApis(diagnosticResults, analysisContext);
			ProjectReport projectReport = _reportBuilder.BuildReport(diagnosticsWithApis, analysisContext, project, cancellation);

			return (RunResult.RequirementsNotMet, projectReport, diagnosticsWithApis);
		}

		private CodeSourceReport CreateCodeSourceReport(AppAnalysisContext analysisContext, List<ProjectReport> projectReports, IEnumerable<Api> allDistinctApis)
		{
			if (analysisContext.IncludeAllDistinctApis) 
			{
				var sortedDistinctApiLines = allDistinctApis.OrderBy(api => api.FullName)
															.Select(api => new Line(api.FullName))
															.ToList();

				var codeSourceReport = new CodeSourceReport(analysisContext.CodeSource.Location, sortedDistinctApiLines, projectReports);
				return codeSourceReport;
			}
			else
			{
				int allDistinctApisCount = allDistinctApis.Count();
				var codeSourceReport = new CodeSourceReport(analysisContext.CodeSource.Location, allDistinctApisCount, projectReports);
				return codeSourceReport;
			}
		}

		[SuppressMessage("CodeQuality", "Serilog004:Constant MessageTemplate verifier", Justification = "Ok to use runtime dependent new line in message")]
		private void OnAnalyzerException(Exception exception, DiagnosticAnalyzer analyzer, Diagnostic diagnostic)
		{
			var prettyLocation = diagnostic.Location.GetMappedLineSpan().ToString();

			string errorMsg = $"Analyzer error:{Environment.NewLine}{{Id}}{Environment.NewLine}{{Location}}{Environment.NewLine}{{Analyzer}}";
			Log.Error(exception, errorMsg, diagnostic.Id, prettyLocation, analyzer.ToString());
		}
	}
}