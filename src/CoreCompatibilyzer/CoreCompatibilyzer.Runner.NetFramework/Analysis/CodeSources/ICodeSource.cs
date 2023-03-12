using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace CoreCompatibilyzer.Runner.Analysis.CodeSources
{
    internal interface ICodeSource
    {
        CodeSourceType Type { get; }

        string Location { get; }

        //Task<Solution?> LoadSolutionAsync(MSBuildWorkspace workspace, CancellationToken cancellationToken);
    }
}
