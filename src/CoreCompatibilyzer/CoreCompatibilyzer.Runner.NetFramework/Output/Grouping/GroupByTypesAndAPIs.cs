using System;
using System.Collections.Generic;
using System.Threading;

using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Runner.Output.Data;

namespace CoreCompatibilyzer.Runner.Output
{
	/// <summary>
	/// Results grouper by types and/or API grouping modes.
	/// </summary>
	internal sealed class GroupByTypesAndAPIs : GroupLinesBase
	{
		public GroupByTypesAndAPIs(GroupingMode grouping) : base(grouping) 
		{ }

		/// <summary>
		/// Group results by types and/or APIs.
		/// </summary>
		/// <param name="analysisContext">Analysis context.</param>
		/// <param name="diagnosticsWithApis">The diagnostics with APIs.</param>
		/// <param name="projectDirectory">Pathname of the project directory.</param>
		/// <param name="cancellation">Cancellation token.</param>
		/// <returns>
		/// Output API results grouped by types and/or APIs specified by grouping modes.
		/// </returns>
		public override IEnumerable<ReportGroup> GetApiGroups(AppAnalysisContext analysisContext, DiagnosticsWithBannedApis diagnosticsWithApis,
															  string? projectDirectory, CancellationToken cancellation)
		{

		}
	}
}
