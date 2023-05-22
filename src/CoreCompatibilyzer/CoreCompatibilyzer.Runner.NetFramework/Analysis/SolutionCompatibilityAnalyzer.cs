using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CoreCompatibilyzer.BannedApiData.Providers;
using CoreCompatibilyzer.BannedApiData.Storage;
using CoreCompatibilyzer.Constants;
using CoreCompatibilyzer.DotNetRuntimeVersion;
using CoreCompatibilyzer.Runner.Analysis.Helpers;
using CoreCompatibilyzer.Runner.Input;
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
		private ImmutableArray<DiagnosticAnalyzer> _diagnosticAnalyzers;
		private readonly IBannedApiStorage _bannedApiStorage;

		private bool IsBannedStorageInitAndNonEmpty => _bannedApiStorage?.BannedApiKindsCount > 0;

		private SolutionCompatibilityAnalyzer(IBannedApiStorage bannedApiStorage, ImmutableArray<DiagnosticAnalyzer> diagnosticAnalyzers)
        {
            _bannedApiStorage	 = bannedApiStorage;
			_diagnosticAnalyzers = diagnosticAnalyzers;
        }

		public static async Task<SolutionCompatibilityAnalyzer> CreateAnalyzer(CancellationToken cancellationToken, 
																			   IBannedApiDataProvider? customBannedApiDataProvider = null)
		{
			var bannedApiStorage = await BannedApiStorage.GetStorageAsync(cancellationToken, customBannedApiDataProvider)
														 .ConfigureAwait(false);
			var diagnosticAnalyzers = CollectAnalyzers();
			return new SolutionCompatibilityAnalyzer(bannedApiStorage, diagnosticAnalyzers);
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

			foreach (Project project in solution.Projects)
			{
				Log.Information("Started validation of the project \"{ProjectName}\".", project.Name);

				if (cancellationToken.IsCancellationRequested)
				{
					Log.Information("Finished validation of the project \"{ProjectName}\". Project valudation result: {Result}.", 
									project.Name, RunResult.Cancelled);
					solutionValidationResult = solutionValidationResult.Combine(RunResult.Cancelled);
					return solutionValidationResult;
				}

				var projectValidationResult = await AnalyseProject(project, analysisContext, cancellationToken);
				solutionValidationResult = solutionValidationResult.Combine(projectValidationResult);

				Log.Information("Finished validation of the project \"{ProjectName}\". Project valudation result: {Result}.", 
								project.Name, projectValidationResult);
			}

			return solutionValidationResult;
		}

		private async Task<RunResult> AnalyseProject(Project project, AppAnalysisContext analysisContext, CancellationToken cancellationToken)
		{
			Log.Debug("Obtaining Roslyn compilation data for the project \"{ProjectName}\".", project.Name);
			var compilation = await project.GetCompilationAsync(cancellationToken);

			if (compilation == null)
			{
				Log.Error("Failed to obtain Roslyn compilation data for the project with name \"{ProjectName}\" and path \"{ProjectPath}\".",
						  project.Name, project.FilePath);
				return RunResult.RunTimeError;
			}

			Log.Debug("Obtained Roslyn compilation data for the project \"{ProjectName}\" successfully.", project.Name);
			Log.Debug("Obtaining .Net runtime version targeted by the project \"{ProjectName}\".", project.Name);

			var dotNetVersion = DotNetVersionsStorage.Instance.GetDotNetRuntimeVersion(compilation);
			var versionValidationResult = ValidateProjectVersion(project, dotNetVersion, analysisContext.TargetRuntime);

			if (versionValidationResult.HasValue)
				return versionValidationResult.Value;

			if (!IsBannedStorageInitAndNonEmpty)
				return RunResult.Success;

			bool useSuppressionMechanism = !analysisContext.DisableSuppressionMechanism;
			var analysisValidationResult = await RunAnalyzersOnProjectAsync(compilation, useSuppressionMechanism, cancellationToken).ConfigureAwait(false);
			return analysisValidationResult;
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

		private async Task<RunResult> RunAnalyzersOnProjectAsync(Compilation compilation, bool useSuppressionMechanism, CancellationToken cancellation)
		{
			if (_diagnosticAnalyzers.IsDefaultOrEmpty)
				return RunResult.Success;

			SuppressionManager.UseSuppression = useSuppressionMechanism;
			var compilationAnalysisOptions = new CompilationWithAnalyzersOptions(options: null!, OnAnalyzerException,
																				 concurrentAnalysis: true, logAnalyzerExecutionTime: false);
			CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(compilationAnalysisOptions, _diagnosticAnalyzers, cancellation);

			var diagnosticResults = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(cancellation).ConfigureAwait(false);

			if (diagnosticResults.IsDefaultOrEmpty)
				return RunResult.Success;

			foreach (Diagnostic diagnostic in diagnosticResults)
			{
				LogErrorForFoundDiagnostic(diagnostic);
			}

			return RunResult.RequirementsNotMet;
		}

		[SuppressMessage("CodeQuality", "Serilog004:Constant MessageTemplate verifier", Justification = "Ok to use runtime dependent new line in message")]
		private void OnAnalyzerException(Exception exception, DiagnosticAnalyzer analyzer, Diagnostic diagnostic)
		{
			var prettyLocation = diagnostic.Location.GetMappedLineSpan().ToString();

			string errorMsg = $"Analyzer error:{Environment.NewLine}{{Id}}{Environment.NewLine}{{Location}}{Environment.NewLine}{{Analyzer}}";
			Log.Error(exception, errorMsg, diagnostic.Id, prettyLocation, analyzer.ToString());
		}

		
		private void LogErrorForFoundDiagnostic(Diagnostic diagnostic)
		{
			if (diagnostic.Properties.Count == 0 || 
				!diagnostic.Properties.TryGetValue(CommonConstants.ApiNameDiagnosticProperty, out string? fullApiName) || 
				fullApiName.IsNullOrWhiteSpace())
			{
				LogMessage(diagnostic.Severity, diagnostic.ToString(), messageArgs: null);
				return;
			}

			var prettyLocation = diagnostic.Location.GetMappedLineSpan().ToString();
			var diagnosticMessage = string.Format(diagnostic.Descriptor.Title.ToString(), fullApiName);
			string errorMsgTemplate = $"{{Id}} {{Severity}} {{Location}}:{Environment.NewLine}{{Description}}";

			LogMessage(diagnostic.Severity, errorMsgTemplate, diagnostic.Id, diagnostic.Severity, prettyLocation, diagnosticMessage);
		}

		[SuppressMessage("CodeQuality", "Serilog004:Constant MessageTemplate verifier", Justification = "Ok to use runtime dependent new line in message")]
		private void LogMessage(DiagnosticSeverity severity, string message, params object[]? messageArgs)
		{
			switch (severity)
			{
				case DiagnosticSeverity.Error:
					Log.Error(message, messageArgs);
					return;

				case DiagnosticSeverity.Warning:
					Log.Warning(message, messageArgs);
					break;

				case DiagnosticSeverity.Info:
					Log.Information(message, messageArgs);
					break;

				case DiagnosticSeverity.Hidden:
					Log.Debug(message, messageArgs);
					break;
			}
		}
	}
}