using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Utils.Common;

using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

using Serilog;
using CoreCompatibilyzer.Runner.Output;

namespace CoreCompatibilyzer.Runner.Analysis
{
	/// <summary>
	/// A solution analysis runner that does preparatory work - register MSBuild, load solution for analysis and calls analyzer.
	/// </summary>
	internal class SolutionAnalysisRunner
	{
		public async Task<RunResult> RunAnalysisAsync(AppAnalysisContext analysisContext, CancellationToken cancellationToken)
		{
			analysisContext.ThrowIfNull(nameof(analysisContext));

			if (cancellationToken.IsCancellationRequested)
				return RunResult.Cancelled;
			else if (!TryRegisterMSBuild(analysisContext))
				return RunResult.RunTimeError;

			RunResult runResult = RunResult.Success;
			bool hasErrors = false;

			try
			{
				cancellationToken.ThrowIfCancellationRequested();

				runResult = await LoadAndAnalyzeCodeSourceAsync(analysisContext, cancellationToken);
			}
			catch (OperationCanceledException cancellationException)
			{
				Log.Warning(cancellationException, "The validation of \"{CodeSourcePath}\" for compatibility with .Net Core was cancelled.",
							analysisContext.CodeSource.Location);
				runResult = RunResult.Cancelled;
			}
			catch (Exception exception)
			{
				Log.Error(exception, "An error happened during the analysis of \"{CodeSourcePath}\" for compatibility with .Net Core.", analysisContext.CodeSource.Location);
				hasErrors = true;
			}
			finally
			{
				if (!TryUnregisterMSBuild())
					hasErrors = true;
			}

			return hasErrors
				? RunResult.RunTimeError
				: runResult;
		}

		private async Task<RunResult> LoadAndAnalyzeCodeSourceAsync(AppAnalysisContext analysisContext, CancellationToken cancellationToken)
		{
			Log.Information("Start analyzing the code source \"{CodeSourcePath}\".", analysisContext.CodeSource.Location);

			using var workspace = MSBuildWorkspace.Create();

			try
			{
				workspace.WorkspaceFailed += OnCodeSourceLoadError;

				Log.Information("Start loading the code source \"{CodeSourcePath}\".", analysisContext.CodeSource.Location);
				var solution = await analysisContext.CodeSource.LoadSolutionAsync(workspace, cancellationToken)
															   .ConfigureAwait(false);
				if (solution == null)
				{
					Log.Error("Failed to load solution from the code source \"{CodeSourcePath}\".", analysisContext.CodeSource.Location);
					return RunResult.RunTimeError;
				}

				Log.Information("Successfully loaded the code source \"{CodeSourcePath}\".", analysisContext.CodeSource.Location);
				Log.Debug("Count of loaded projects: {ProjectsCount}.", solution.ProjectIds.Count);

				Log.Information("Load banned API data and initialize analyzers");
				var solutionCompatibilityAnalyzer = await SolutionCompatibilityAnalyzer.CreateAnalyzer(cancellationToken)
																					   .ConfigureAwait(false);
				Log.Information("Start validating the solution.");

				RunResult validationResult = await solutionCompatibilityAnalyzer.AnalyseSolution(solution, analysisContext, cancellationToken);
				
				Log.Information("Successfully finished validating the solution.");
				return validationResult;
			}
			finally
			{
				workspace.WorkspaceFailed -= OnCodeSourceLoadError;
			}
		}

		private void OnCodeSourceLoadError(object sender, WorkspaceDiagnosticEventArgs e)
		{
			switch (e.Diagnostic.Kind)
			{
				case WorkspaceDiagnosticKind.Failure:
					Log.Error("{WorkspaceDiagnostic}", e.Diagnostic);
					break;
				case WorkspaceDiagnosticKind.Warning:
					Log.Warning("{WorkspaceDiagnostic}", e.Diagnostic);
					break;
			}
		}

		private bool TryRegisterMSBuild(AppAnalysisContext analysisContext)
		{
			if (analysisContext.MSBuildPath != null)
			{
				return TryRegisterMSBuildByPath(analysisContext.MSBuildPath);
			}

			Log.Information("Searching for MSBuild instances installed on the current machine.");

			var vsInstances = MSBuildLocator.QueryVisualStudioInstances();
			VisualStudioInstance? latestVSInstance = vsInstances.OrderByDescending(vsInstance => vsInstance.Version)
																.FirstOrDefault();
			if (latestVSInstance == null)
			{
				Log.Error("No installed MSBuild version was found on the machine.");
				return false;
			}

			Log.Information("Found MSBuild instance with name \"{VisualStudioName}\", version \"{VisualStudioVersion}\".", latestVSInstance.Name, latestVSInstance.Version);
			Log.Information("MSBuildPath: \"{MSBuildPath}\".", latestVSInstance.MSBuildPath);

			try
			{
				MSBuildLocator.RegisterInstance(latestVSInstance);
				return true;
			}
			catch (Exception e)
			{
				Log.Error(e, "Error during registration of MSBuild instance.");
				return false;
			}
		}

		private bool TryRegisterMSBuildByPath(string msBuildPath)
		{
			try
			{
				Log.Information("Registering MSBuild instance at the provided path \"{MSBuildPath}\".", msBuildPath);

				string? msBuildDir = Path.GetDirectoryName(msBuildPath);
				MSBuildLocator.RegisterMSBuildPath(msBuildDir);

				Log.Information("Successfully registered MSBuild instance at the provided path \"{MSBuildPath}\".", msBuildPath);
				return true;
			}
			catch (Exception e)
			{
				Log.Error(e, "Error during the registration of MSBuild instance. Failed to register MSBuild using a provided path \"{MSBuildPath}\".", msBuildPath);
				return false;
			}
		}

		private bool TryUnregisterMSBuild()
		{
			try
			{
				MSBuildLocator.Unregister();
				return true;
			}
			catch (Exception e)
			{
				Log.Error(e, "Error on attempt to unregister MSBuild.");
				return false;
			}
		}
	}
}