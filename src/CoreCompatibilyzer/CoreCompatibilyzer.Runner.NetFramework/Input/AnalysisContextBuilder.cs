using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using CoreCompatibilyzer.Utils.Common;
using CoreCompatibilyzer.Runner.Constants;
using CoreCompatibilyzer.Runner.Analysis.CodeSources;

namespace CoreCompatibilyzer.Runner.Input
{
    internal class AnalysisContextBuilder
	{
		public AnalysisContext CreateContext(CommandLineOptions commandLineOptions)
		{
			commandLineOptions.ThrowIfNull(nameof(commandLineOptions));

			var codeSource = ReadCodeSource(commandLineOptions.CodeSource);

			if (codeSource == null)
				throw new ArgumentException("Code source is not specified");

			var input = new AnalysisContext(codeSource, commandLineOptions.MSBuildPath);
			return input;
		}

		private ICodeSource? ReadCodeSource(string codeSourceLocation)
		{
			if (codeSourceLocation.IsNullOrWhiteSpace())
				return null;

			if (!File.Exists(codeSourceLocation))
				throw new ArgumentException($"Code source to use in validation is not found at {codeSourceLocation}");

			string fullPath = Path.GetFullPath(codeSourceLocation);
			ICodeSource codeSource = CreateCodeSource(fullPath);
			return codeSource;
		}

		private ICodeSource CreateCodeSource(string codeSourceLocation)
		{
			string extension = Path.GetExtension(codeSourceLocation);

			return extension switch
			{
				CommonConstants.ProjectFileExtension  => new ProjectCodeSource(codeSourceLocation),
				CommonConstants.SolutionFileExtension => new SolutionCodeSource(codeSourceLocation),
				_ => throw new ArgumentException($"Not supported code source {codeSourceLocation}. You can specify only C# projects (*.csproj) and solutions (*.sln) as code sources.")
			};
		}
	}
}
