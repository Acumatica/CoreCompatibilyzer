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
		/// Currently, the supported code sources are C# projects and C# solutions.
		/// </remarks>
		[Value(index: 0, MetaName = CommandLineArgNames.CodeSource, Required = true,
			   HelpText = "A path to the \"code source\" which will be validated. The term \"code source\" is a generalization for components/services that can provide source code to the tool.\n" +
						  "Currently, the supported code sources are C# projects and C# solutions.")]
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
				HelpText = "When this optional flag is specified, the code analysis would not take into consideration suppression comments present in the code " +
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
		/// Include details about banned API usages locations. By default, the report includes only a shortened list of used banned API. <br/>
		/// When this flag is set, the report will include much more details about the location of each banned API call.
		/// </summary>
		[Option(longName: CommandLineArgNames.IncludeApiUsages,
				HelpText = "By default, the report output includes only a shortened list of used banned API.\n" +
						   "Set this flag to include the locations of used banned API calls into the report.")]
		public bool IncludeApiUsages { get; }

		/// <summary>
		/// If this flag is set to <see langword="true"/> then the report will start with a list of all distinct APIs used by the code source.
		/// </summary>
		[Option(longName: CommandLineArgNames.IncludeAllDistinctApis,
				HelpText = "If this flag is set to true then the report will start with a list of all distinct APIs used by the code source.")]
		public bool IncludeAllDistinctApis { get; }

		/// <summary>
		/// When report is displayed in a shortened form without banned API calls locations, it could be shortened even more.<br/>
		///	By default, the report will not display used banned type member APIs if their containing type is also banned and used by the code being analyzed.<br/>
		///	Set this flag to include the banned type member APIs into the report together with their containing type.<br/>
		///	This flag does not affect the report when the <see cref="IncludeApiUsages"/> is set.
		/// </summary>
		[Option(longName: CommandLineArgNames.ShowMembersOfUsedType,
				HelpText = "When report is displayed in a shortened form without banned API calls locations, it could be shortened even more.\n" +
						   "By default, the report will not display used banned type member APIs if their containing type is also banned and used by the code being analyzed.\n" +
						   "Set this flag to include the banned type member APIs into the report together with their containing type.\n" +
						   $"This flag does not affect the report when the --{CommandLineArgNames.IncludeApiUsages} is specified.")]
		public bool ShowMembersOfUsedType { get; }

		/// <summary>
		/// The report grouping. By default, there is no grouping. You can make grouping by source file paths, namespaces, types, APIs, or by any combination of them:<br/>
		///	- Add "<c>f</c>" or "<c>F</c>" to group results by source file.<br/>
		///	- Add "<c>n</c>" or "<c>N</c>" to group results by namespaces,<br/>
		///	- Add "<c>t</c>" or "<c>T</c>" to group results by types,<br/>
		/// - Add "<c>a</c>" or "<c>A</c>" to group API usages by APIs.<br/><br/>
		///	Any combination of these characters will specify a report grouping. For example, specify both "<c>ftn</c>" to group results by files, types and namespaces.<br/>
		/// </summary>
		/// <remarks>
		/// Reports grouping works like this:<br/>
		/// - First, reports are grouped by filepaths, if "<c>f</c>" or "<c>F</c>" is specified in the grouping.<br/>
		/// - Second, reports are grouped by namespaces, if "<c>n</c>" or "<c>N</c>" is specified in the grouping.<br/>
		/// - Third, reports are grouped by types, if "<c>t</c>" or "<c>T</c>" is specified in the grouping.<br/>
		/// - Fourth, reports are grouped by APIs, if "<c>a</c>" or "<c>A</c>" is specified in the grouping.<br/>
		/// </remarks>
		[Option(shortName: CommandLineArgNames.ReportGroupingShort, longName: CommandLineArgNames.ReportGroupingLong,
				HelpText = """
		The report grouping. By default, there is no grouping. You can make grouping by source file paths, namespaces, types, APIs, or by any combination of them:
		  - Add "f" or "F" to group results by source file.
		  - Add "n" or "N" to group results by namespaces,
		  - Add "t" or "T" to group results by types,
		  - Add "a" or "A" to group API usages by APIs.

		Any combination of these characters will specify a report grouping. For example, specify both "ftn" to group results by files, types and namespaces.
		
		Reports grouping works like this:
		  - First, reports are grouped by filepaths, if "f" or "F" is specified in the grouping.
		  - Second, reports are grouped by namespaces, if "n" or "N" is specified in the grouping.
		  - Third, reports are grouped by types, if "t" or "T" is specified in the grouping.
		  - Fourth, reports are grouped by APIs, if "a" or "A" is specified in the grouping.
		""")]
		public string? ReportGrouping { get; }

		/// <summary>
		/// The name of the output file. If not specified then the report will be outputted to the console window.
		/// </summary>
		[Option(shortName: CommandLineArgNames.OutputFileShort, longName: CommandLineArgNames.OutputFileLong,
				HelpText = "The name of the output file. If not specified then the report will be outputted to the console window.")]
		public string? OutputFileName { get; }

		/// <summary>
		/// When report is set to output the detailed list of banned APIs with their usages this flag regulates how the locations of API usages will be output.<br/>
		///	By default, file paths in locations are relative to the containing project directory. However, if this flag is set then the absolute file paths will be used.<br/>
		///	This flag does not affect the report when the <see cref="IncludeApiUsages"/> is not set.
		/// </summary>
		[Option(longName: CommandLineArgNames.OutputAbsolutePathsToUsages,
				HelpText = "When report is set to output the detailed list of banned APIs with their usages this flag regulates how the locations of API usages will be output.\n" +
						   "By default, file paths in locations are relative to the containing project directory. " +
						   "However, if this flag is set then the absolute file paths will be used.\n" +
						  $"This flag does not affect the report when the --{CommandLineArgNames.IncludeApiUsages} is not specified.")]
		public bool OutputAbsolutePathsToUsages { get; }

		/// <summary>
		/// The report output format. There are two supported values:
		/// <list type="bullet">
		/// <item>"text" to output the report in plain text, this is the default output mode,</item>
		/// <item>"json" to output the report in JSON format.</item>
		/// </list>
		/// </summary>
		[Option(longName: CommandLineArgNames.OutputFormat,
				HelpText = "The report output format. There are two supported values:\n" +
						   "- \"text\" to output the report in plain text, this is the default output mode,\n" +
						   "- \"json\" to output the report in JSON format.")]
		public string? OutputFormat { get; }

		// Constructor arguments order must be the same as the properties order. This allows command line parser to initialize immutable options object via constructor.
		// See this for details: https://github.com/commandlineparser/commandline/wiki/Immutable-Options-Type
		public CommandLineOptions(string codeSource, string? verbosity, bool disableSuppressionMechanism, string? msBuildPath, bool includeApiUsages, bool includeAllDistinctApis,
								  bool showMembersOfUsedType, string? reportGrouping, string? outputFileName, bool outputAbsolutePathsToUsages, string? outputFormat)
		{
			CodeSource 					= codeSource;
			Verbosity 					= verbosity;
			DisableSuppressionMechanism = disableSuppressionMechanism;
			MSBuildPath 				= msBuildPath;
			IncludeApiUsages			= includeApiUsages;
			IncludeAllDistinctApis		= includeAllDistinctApis;
			ShowMembersOfUsedType		= showMembersOfUsedType;
			ReportGrouping 				= reportGrouping;
			OutputFileName				= outputFileName;
			OutputAbsolutePathsToUsages = outputAbsolutePathsToUsages;
			OutputFormat				= outputFormat;
		}
	}
}