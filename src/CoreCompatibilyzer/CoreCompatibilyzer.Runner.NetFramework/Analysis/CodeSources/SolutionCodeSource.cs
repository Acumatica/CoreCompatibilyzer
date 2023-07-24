using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CoreCompatibilyzer.Utils.Common;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace CoreCompatibilyzer.Runner.Analysis.CodeSources
{
    internal class SolutionCodeSource : ICodeSource
    {
        public CodeSourceType Type => CodeSourceType.Solution;

        public string Location { get; }

        public SolutionCodeSource(string solutionPath)
        {
            Location = solutionPath.ThrowIfNullOrWhiteSpace(nameof(solutionPath));
        }

		public Task<Solution> LoadSolutionAsync(MSBuildWorkspace workspace, CancellationToken cancellationToken) =>
			workspace.OpenSolutionAsync(Location, cancellationToken: cancellationToken);

        public IEnumerable<Project> GetProjectsForValidation(Solution solution) =>
            solution.ThrowIfNull(nameof(solution)).Projects;
	}
}
