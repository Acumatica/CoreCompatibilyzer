using System;
using System.Collections.Generic;
using System.Linq;

using Serilog.Events;

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
		/// Optional explicitly specified logger <see cref="LogEventLevel"/> verbosity. <br/>
		/// If null then <see cref="LogEventLevel.Information"/> will be used as default.
		/// </summary>
		/// <value>
		/// The explicitly specified logger's verbosity.
		/// </value>
		[Option(shortName: CommandLineArgNames.VerbosityShort, longName: CommandLineArgNames.MSBuildPath,
				HelpText = "This optional parameter allows you to explicitly specify logger verbosity. The allowed values are taken from the " +
						   nameof(Serilog) + "." + nameof(Serilog.Events) + "." + nameof(Serilog.Events.LogEventLevel) + "enum\n\n. " +
						   "Here is the list of allowed values:\n" +
						   nameof(LogEventLevel.Verbose) + ", " + nameof(LogEventLevel.Debug) + ", " + nameof(LogEventLevel.Information) + ",\n" +
						   nameof(LogEventLevel.Warning) + ", " + nameof(LogEventLevel.Error) + ", " + nameof(LogEventLevel.Fatal) + ".\n\n" +
						   "By defalut the logger will use the " + nameof(LogEventLevel.Information) + " verbosity.")]
		public string? Verbosity { get; }

		/// <summary>
		/// Optional explicitly specified path to MSBuild. Can be null. If null then MSBuild path is retrieved automatically.
		/// </summary>
		/// <value>
		/// The optional explicitly specified path to MSBuild.
		/// </value>
		[Option(longName: CommandLineArgNames.MSBuildPath,
				HelpText = "This optional parameter allows you to provide explicitly a path to MSBuild tool that will be used for analysis.")]
		public string? MSBuildPath { get; }

		// Constructor arguments order must be the same as the properties order. This allows command line parser to initialize immutable options object via constructor.
		// See this for details: https://github.com/commandlineparser/commandline/wiki/Immutable-Options-Type
		public CommandLineOptions(string codeSource, string? verbosity, string? msBuildPath)
		{
			CodeSource = codeSource;
			Verbosity = verbosity; 
			MSBuildPath = msBuildPath;
		}
	}
}