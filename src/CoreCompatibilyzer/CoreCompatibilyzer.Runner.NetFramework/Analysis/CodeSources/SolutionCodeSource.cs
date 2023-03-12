using System;
using System.Collections.Generic;
using System.Text;

using CoreCompatibilyzer.Utils.Common;

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
    }
}
