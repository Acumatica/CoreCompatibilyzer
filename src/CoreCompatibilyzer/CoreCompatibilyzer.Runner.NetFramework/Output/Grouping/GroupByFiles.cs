using System;
using System.Collections.Generic;
using System.Threading;

using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Runner.Output.Data;

namespace CoreCompatibilyzer.Runner.Output
{
	/// <summary>
	/// Results grouper by any combination of file, namespace, types, and API grouping modes.
	/// </summary>
	internal sealed class GroupByAnyGroupingCombination : GroupLinesBase
	{
		public GroupByAnyGroupingCombination(GroupingMode grouping) : base(grouping)
		{ }


		/// <summary>
		///  Group results by any combination of file, namespace, types, and API grouping modes.
		/// </summary>
		/// <param name="analysisContext">Analysis context.</param>
		/// <param name="diagnosticsWithApis">The diagnostics with APIs.</param>
		/// <param name="projectDirectory">Pathname of the project directory.</param>
		/// <param name="cancellation">Cancellation token.</param>
		/// <returns>
		/// Output API results grouped by any combination of file, namespace, types, and API grouping modes.
		/// </returns>
		public override IEnumerable<ReportGroup> GetApiGroups(AppAnalysisContext analysisContext, DiagnosticsWithBannedApis diagnosticsWithApis,
															  string? projectDirectory, CancellationToken cancellation)
		{

		}
	}
}
