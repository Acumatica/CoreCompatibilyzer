using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Utils.Common;

using Microsoft.CodeAnalysis;

using Serilog;
using System.Runtime;
using CoreCompatibilyzer.DotNetCompatibility;

namespace CoreCompatibilyzer.Runner.Analysis
{
    internal class SolutionCompatibilityAnalyzer
	{
		private readonly DotNetVersionReader _dotNetVersionReader = new DotNetVersionReader();

		public async Task<RunResult> AnalyseSolution(Solution solution, AnalysisContext analysisContext, CancellationToken cancellationToken)
		{
			RunResult solutionValidationResult = RunResult.Success;

			foreach (Project project in solution.Projects)
			{
				Log.Information("Started validation of the project \"{ProjectName}\".", project.Name);

				if (cancellationToken.IsCancellationRequested)
				{
					Log.Information("Project \"{ProjectName}\" validation was cancelled.", project.Name);
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

			var dotNetVersion = _dotNetVersionReader.TryParse(compilation);

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