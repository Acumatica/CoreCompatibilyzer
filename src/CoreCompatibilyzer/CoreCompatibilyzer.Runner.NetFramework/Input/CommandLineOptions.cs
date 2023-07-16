﻿using System;
using System.Collections.Generic;
using System.Linq;

using Serilog.Events;

using CommandLine;

using CoreCompatibilyzer.Runner.Constants;

namespace CoreCompatibilyzer.Runner.Input
{
	internal class CommandLineOptions
	{
		/// <summary>
		/// The code source that will be analysed for the compatibility with the of .Net Core 2.2 runtime.
		/// </summary>
		/// <remarks>
		/// Currenly, the supported code sources are C# projects and C# solutions.
		/// </remarks>
		[Value(index: 0, MetaName = CommandLineArgNames.CodeSource, Required = true,
			   HelpText = "A path to the \"code source\" which will be validated. The term \"code source\" is a generalization for components/services that can provide source code to the tool.\n" +
						  "Currenly, the supported code sources are C# projects and C# solutions.")]
		public string CodeSource { get; }

		/// <summary>
		/// Optional explicitly specified logger <see cref="LogEventLevel"/> verbosity. <br/>
		/// If null then <see cref="LogEventLevel.Information"/> will be used as default.
		/// </summary>
		/// <value>
		/// The explicitly specified logger's verbosity.
		/// </value>
		[Option(shortName: CommandLineArgNames.VerbosityShort, longName: CommandLineArgNames.VerbosityLong,
				HelpText = "This optional parameter allows you to explicitly specify logger verbosity. The allowed values are taken from the \"" +
						  $"{nameof(Serilog)}.{nameof(Serilog.Events)}.{nameof(Serilog.Events.LogEventLevel)} enum.\n\n" +
						   "The allowed values:\n" +
						  $"{nameof(LogEventLevel.Verbose)}, {nameof(LogEventLevel.Debug)}, {nameof(LogEventLevel.Information)}, " +
						  $"{nameof(LogEventLevel.Warning)}, {nameof(LogEventLevel.Error)}, {nameof(LogEventLevel.Fatal)}.\n\n" +
						  $"By default, the logger will use the \"{nameof(LogEventLevel.Information)}\" verbosity.")]
		public string? Verbosity { get; }

		/// <summary>
		/// If this flag is set to true then the code analysis won't take into consideration suppression comments present in the code.
		/// </summary>
		[Option(longName: CommandLineArgNames.DisableSuppressionMechanism,
				HelpText = "When this optional flag is set to true, the code analysis would not take into consideration suppression comments present in the code " +
							"and will report suppressed diagnostics.")]
		public bool DisableSuppressionMechanism { get; }

		/// <summary>
		/// Optional explicitly specified path to MSBuild. Can be null. If null then MSBuild path is retrieved automatically.
		/// </summary>
		/// <value>
		/// The optional explicitly specified path to MSBuild.
		/// </value>
		[Option(longName: CommandLineArgNames.MSBuildPath,
				HelpText = "This optional parameter allows you to provide explicitly a path to the MSBuild tool that will be used for analysis.\n" +
						   "By default, MSBuild installations will be searched automatically on the current machine and the latest found version will be used.")]
		public string? MSBuildPath { get; }

		/// <summary>
		/// The report format. Two options are available:<br/>
		/// - <see cref="FormatArgsConstants.UsedAPIsOnly"/>: Format Mode to output only a shortened list of used banned API.<br/>
		/// - <see cref="FormatArgsConstants.UsedAPIsWithUsages"/>: Format Mode to output only a detailed list of used banned APIs with usages locations.
		/// </summary>
		[Option(shortName: CommandLineArgNames.ReportFormatShort, longName: CommandLineArgNames.ReportFormatLong,
				HelpText = "This parameter allows you to specify the report output format. There are two available modes:\n" +
						  $"- Output only a shortened list of used banned APIs. Pass \"{FormatArgsConstants.UsedAPIsOnly}\" value to use this mode.\n" + 
						  $"- Output only a detailed list of used banned APIs with usages locations. Pass \"{FormatArgsConstants.UsedAPIsWithUsages}\" value to use this mode.\n\n" +
						  $"The default format value is \"{FormatArgsConstants.UsedAPIsOnly}\".")]
		public string? ReportFormat { get; }

		/// <summary>
		/// The report grouping. By default there is no grouping. You can make grouping of the reported API by namespaces, types or both:<br/>
		///	- Add "<c>n</c>" or "<c>N</c>" to group results by namespaces,<br/>
		///	- Add "<c>t</c>" or "<c>T</c>" to group results by types,<br/>
		///	- Add both to group results by both types and namespaces.
		/// </summary>
		[Option(shortName: CommandLineArgNames.ReportGroupingShort, longName: CommandLineArgNames.ReportGroupingLong,
				HelpText = "This parameter allows you to specify the report grouping. By default there is no grouping. " +
						   "You can make grouping of the reported API by namespaces, types or both:\n" +
						   "- Add \"n\" or \"N\" to group results by namespaces,\n" +
						   "- Add \"t\" or \"T\" to group results by types,\n" +
						   "- Add both to group results by both types and namespaces.")]
		public string? ReportGrouping { get; }

		// Constructor arguments order must be the same as the properties order. This allows command line parser to initialize immutable options object via constructor.
		// See this for details: https://github.com/commandlineparser/commandline/wiki/Immutable-Options-Type
		public CommandLineOptions(string codeSource, string? verbosity, bool disableSuppressionMechanism, string? msBuildPath, 
								  string? reportFormat, string? reportGrouping)
		{
			CodeSource 					= codeSource;
			Verbosity 					= verbosity;
			DisableSuppressionMechanism = disableSuppressionMechanism;
			MSBuildPath 				= msBuildPath;
			ReportFormat 				= reportFormat;
			ReportGrouping 				= reportGrouping;
		}
	}
}