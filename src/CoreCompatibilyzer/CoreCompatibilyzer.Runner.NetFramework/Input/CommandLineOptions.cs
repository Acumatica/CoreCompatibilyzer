using System;
using System.Collections.Generic;
using System.Linq;

using CommandLine;

using CoreCompatibilyzer.Runner.Constants;

namespace CoreCompatibilyzer.Runner.Input
{
	internal class CommandLineOptions
	{
		[Option(longName: CommandLineArgNames.CodeSource, Required = true,
				HelpText = "A path to the \"code source\" which will be validated. The term \"code source\" is a generalization for components/services that can provide source code to the tool.\n" +
						   "Currenly the supported code sources are C# projects and C# solutions.")]
		public string CodeSource { get; }

		/// <summary>
		/// Optional explicitly specified path to MSBuild. Can be null. If null then MSBuild path is retrieved automatically.
		/// </summary>
		/// <value>
		/// The optional explicitly specified path to MSBuild.
		/// </value>
		[Option(longName: CommandLineArgNames.MSBuildPath,
				HelpText = "This parameter allows you to provide explicitly a path to MSBuild tool that will be used for analysis.")]
		public string? MSBuildPath { get; }

		// Constructor arguments order must be the same as the properties order. This allows command line parser to initialize immutable options object via constructor.
		// See this for details: https://github.com/commandlineparser/commandline/wiki/Immutable-Options-Type
		public CommandLineOptions(string codeSource, string? msBuildPath)
		{
			CodeSource = codeSource;
			MSBuildPath = msBuildPath;
		}
	}
}
