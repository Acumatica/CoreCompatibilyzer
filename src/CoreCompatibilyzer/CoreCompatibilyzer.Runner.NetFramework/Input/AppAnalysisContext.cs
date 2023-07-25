using System;

using CoreCompatibilyzer.DotNetRuntimeVersion;
using CoreCompatibilyzer.Runner.Analysis.CodeSources;
using CoreCompatibilyzer.Runner.Output;
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

		/// <inheritdoc cref="CommandLineOptions.ReportFormat"/>
		public FormatMode Format { get; }

		/// <inheritdoc cref="CommandLineOptions.ReportGrouping"/>
		public GroupingMode Grouping { get; }

		/// <inheritdoc cref="CommandLineOptions.ShowMembersOfUsedType"/>
		public bool ShowMembersOfUsedType { get; }

		/// <inheritdoc cref="CommandLineOptions.OutputFileName"/>
		public string? OutputFileName { get; }

		public AppAnalysisContext(ICodeSource codeSource, DotNetRuntime targetRuntime, bool disableSuppressionMechanism, string? msBuildPath,
								  FormatMode formatMode, GroupingMode groupingMode, bool showMembersOfUsedType, string? outputFileName)
		{
			CodeSource 					= codeSource.ThrowIfNull(nameof(codeSource));
			TargetRuntime 				= targetRuntime;
			DisableSuppressionMechanism = disableSuppressionMechanism;
			MSBuildPath 				= msBuildPath.NullIfWhiteSpace();
			Format 						= formatMode;
			Grouping 					= groupingMode;
			ShowMembersOfUsedType		= showMembersOfUsedType;
			OutputFileName				= outputFileName.NullIfWhiteSpace();
		}
	}
}