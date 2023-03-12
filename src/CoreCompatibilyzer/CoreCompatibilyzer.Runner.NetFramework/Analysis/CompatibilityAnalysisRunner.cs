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

using Serilog;

namespace CoreCompatibilyzer.Runner.Analysis
{
	internal class CompatibilityAnalysisRunner
	{
		public async Task<RunResult> Analyze(AnalysisContext analysisContext, CancellationToken cancellationToken)
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

				runResult = await LoadAndAnalyzeCodeSource(analysisContext, cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException cancellationException)
			{
				Log.Warning(cancellationException, "The validation of \"{CodeSourcePath}\" for compatibility with .Net Core was cancelled",
							analysisContext.CodeSource.Location);
				runResult = RunResult.Cancelled;
			}
			catch (Exception exception)
			{
				Log.Error(exception, "An error happened during the analysis of \"{CodeSourcePath}\" for compatibility with .Net Core", analysisContext.CodeSource.Location);
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

		private async Task<RunResult> LoadAndAnalyzeCodeSource(AnalysisContext analysisContext, CancellationToken cancellationToken)
		{

		}

		private bool TryRegisterMSBuild(AnalysisContext analysisContext)
		{
			if (analysisContext.MSBuildPath != null)
			{
				return TryRegisterMSBuildByPath(analysisContext.MSBuildPath);
			}

			var vsInstances = MSBuildLocator.QueryVisualStudioInstances();
			VisualStudioInstance? latestVSInstance = vsInstances.OrderByDescending(vsInstance => vsInstance.Version)
																.FirstOrDefault();
			if (latestVSInstance == null)
			{
				Log.Error("No installed MSBuild version was found on the machine");
				return false;
			}

			Log.Information("Found MSBuild instance with name {VisualStudioName}, version {VisualStudioVersion}", latestVSInstance.Name, latestVSInstance.Version);
			Log.Information("MSBuildPath: {MSBuildPath}", latestVSInstance.MSBuildPath);

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
				string? msBuildDir = Path.GetDirectoryName(msBuildPath);
				MSBuildLocator.RegisterMSBuildPath(msBuildDir);

				Log.Information("Successfully registered MSBuild instance at provided MSBuildPath: {MSBuildPath}", msBuildPath);
				return true;
			}
			catch (Exception e)
			{
				Log.Error(e, "Error during registration of MSBuild instance. Failed to register MSBuild using a provided path {MSBuildPath}", msBuildPath);
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