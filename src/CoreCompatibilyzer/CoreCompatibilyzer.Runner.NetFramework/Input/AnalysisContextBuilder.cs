using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using CoreCompatibilyzer.DotNetCompatibility;
using CoreCompatibilyzer.Runner.Analysis.CodeSources;
using CoreCompatibilyzer.Runner.Constants;
using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.Runner.Input
{
    internal class AnalysisContextBuilder
	{
		private static readonly Dictionary<string, DotNetRuntime> _argumentsToVersionMapping =
			new(StringComparer.OrdinalIgnoreCase)
			{
				{ TargetDotNetVersions.Core21, DotNetRuntime.DotNetCore21 },
				{ TargetDotNetVersions.Core22, DotNetRuntime.DotNetCore22 },
			};

		public AnalysisContext CreateContext(CommandLineOptions commandLineOptions)
		{
			commandLineOptions.ThrowIfNull(nameof(commandLineOptions));
			commandLineOptions.TargetRuntime.ThrowIfNullOrWhiteSpace(nameof(commandLineOptions.TargetRuntime),
																	 message: "The target .Net runtime version is not specified");

			if (!_argumentsToVersionMapping.TryGetValue(commandLineOptions.TargetRuntime, out DotNetRuntime targetRuntime))
			{
				throw new ArgumentOutOfRangeException(paramName: nameof(commandLineOptions.TargetRuntime),
													  actualValue: commandLineOptions.TargetRuntime,
													  message: "The specified .Net runtime version is not supported");
			}

			var codeSource = ReadCodeSource(commandLineOptions.CodeSource);

			if (codeSource == null)
				throw new ArgumentException("Code source is not specified");

			var input = new AnalysisContext(codeSource, targetRuntime, commandLineOptions.MSBuildPath);
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
