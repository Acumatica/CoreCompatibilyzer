using System;
using CoreCompatibilyzer.Runner.Analysis.CodeSources;
using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.Runner.Input
{
    internal class AnalysisContext
	{
		/// <summary>
		/// Gets the code source to validate.
		/// </summary>
		/// <value>
		/// The code source to validate.
		/// </value>
		public ICodeSource CodeSource { get; }

		/// <summary>
		/// Optional explicitly specified path to MSBuild. Can be null. If null then MSBuild path is retrieved automatically.
		/// </summary>
		/// <value>
		/// The optional explicitly specified path to MSBuild.
		/// </value>
		public string? MSBuildPath { get; }


		public AnalysisContext(ICodeSource codeSource, string? msBuildPath)
		{
			CodeSource = codeSource.ThrowIfNull(nameof(codeSource));
			MSBuildPath = msBuildPath.NullIfWhiteSpace();
		}
	}
}
