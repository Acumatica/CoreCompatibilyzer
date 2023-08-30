using System;
using System.Runtime.InteropServices;

using CoreCompatibilyzer.DotNetRuntimeVersion;
using CoreCompatibilyzer.Runner.Analysis.CodeSources;
using CoreCompatibilyzer.Runner.Output.Data;
using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.Runner.Input
{
    internal class AppAnalysisContext
	{
		/// <summary>
		/// Gets the code source to validate.
		/// </summary>
		/// <value>
		/// The code source to validate.
		/// </value>
		public ICodeSource CodeSource { get; }

		/// <summary>
		/// Gets target .Net runtime version that will be used for the compatibility checks.
		/// </summary>
		/// <value>
		/// The target .Net runtime version that will be used for the compatibility checks.
		/// </value>
		public DotNetRuntime TargetRuntime { get; }

		/// <inheritdoc cref="CommandLineOptions.MSBuildPath"/>
		public string? MSBuildPath { get; }

		/// <inheritdoc cref="CommandLineOptions.DisableSuppressionMechanism"/>
		public bool DisableSuppressionMechanism { get; }

		/// <summary>
		/// The report mode. There are two modes:
		/// <list type="bullet">
		/// <item>The default mode <see cref="ReportMode.UsedAPIsOnly"/>, in which the report includes only a shortened list of used banned API.</item>
		/// <item>The <see cref="ReportMode.UsedAPIsWithUsages"/> mode, in which the the report will include much more details about the location of each banned API call.</item>
		/// </list>
		/// </summary>
		public ReportMode ReportMode { get; }

		/// <inheritdoc cref="CommandLineOptions.IncludeAllDistinctApis"/>
		public bool IncludeAllDistinctApis { get; }

		/// <inheritdoc cref="CommandLineOptions.ReportGrouping"/>
		public GroupingMode Grouping { get; }

		/// <inheritdoc cref="CommandLineOptions.ShowMembersOfUsedType"/>
		public bool ShowMembersOfUsedType { get; }

		/// <inheritdoc cref="CommandLineOptions.OutputFileName"/>
		public string? OutputFileName { get; }

		/// <inheritdoc cref="CommandLineOptions.OutputAbsolutePathsToUsages"/>
		public bool OutputAbsolutePathsToUsages { get; }

		/// <inheritdoc cref="CommandLineOptions.OutputFormat"/>
		public OutputFormat OutputFormat { get; }

		/// <summary>
		/// If true then the unnderlying OS is Linux.
		/// </summary>
		public bool IsRunningOnLinux { get; }

		public AppAnalysisContext(ICodeSource codeSource, DotNetRuntime targetRuntime, bool disableSuppressionMechanism, string? msBuildPath,
								  ReportMode reportMode, bool includeAllDistinctApis, GroupingMode groupingMode, bool showMembersOfUsedType, string? outputFileName, 
								  bool outputAbsolutePathsToUsages, OutputFormat outputFormat)
		{
			CodeSource 					= codeSource.ThrowIfNull(nameof(codeSource));
			TargetRuntime 				= targetRuntime;
			DisableSuppressionMechanism = disableSuppressionMechanism;
			MSBuildPath 				= msBuildPath.NullIfWhiteSpace();
			ReportMode 					= reportMode;
			IncludeAllDistinctApis		= includeAllDistinctApis;
			Grouping 					= groupingMode;
			ShowMembersOfUsedType		= showMembersOfUsedType;
			OutputFileName				= outputFileName.NullIfWhiteSpace();
			OutputAbsolutePathsToUsages = outputAbsolutePathsToUsages;
			OutputFormat				= outputFormat;
			IsRunningOnLinux			= RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
		}
	}
}