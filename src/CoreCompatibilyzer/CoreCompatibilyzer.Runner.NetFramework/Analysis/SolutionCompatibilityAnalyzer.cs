using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Utils.Common;

using Microsoft.CodeAnalysis;

using Serilog;
using CoreCompatibilyzer.DotNetRuntimeVersion;

namespace CoreCompatibilyzer.Runner.Analysis
{
    internal class SolutionCompatibilityAnalyzer
	{
		public async Task<RunResult> AnalyseSolution(Solution solution, AnalysisContext analysisContext, CancellationToken cancellationToken)
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

		private async Task<RunResult> AnalyseProject(Project project, AnalysisContext analysisContext, CancellationToken cancellationToken)
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

			if (dotNetVersion == null)
			{
				Log.Error("Failed to get the .Net runtime version targeted by the project with name \"{ProjectName}\" and path \"{ProjectPath}\".",
						  project.Name, project.FilePath);
				return RunResult.RunTimeError;
			}

			Log.Information("Project \"{ProjectName}\" targeted .Net runtime version is \"{ProjectDotNetVersion}\".", project.Name, dotNetVersion.Value);

			if (DotNetRunTimeComparer.Instance.Compare(dotNetVersion.Value, analysisContext.TargetRuntime) >= 0)
			{
				Log.Information("The .Net runtime version \"{ProjectDotNetVersion}\" of the project \"{ProjectName}\" " + 
								"is greater or equals to the target .Net runtime version \"{TargetDotNetVersion}\".",  
								dotNetVersion.Value, project.Name, analysisContext.TargetRuntime);
				return RunResult.Success;
			}

			return RunResult.Success;
		}
	}
}