using System;
using System.Collections.Generic;
using System.Threading;

using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Runner.Output.Data;

namespace CoreCompatibilyzer.Runner.Output
{
	/// <summary>
	/// Results grouper by API grouping mode only.
	/// </summary>
	internal sealed class GroupByOnlyAPIs : GroupLinesBase
	{
		public GroupByOnlyAPIs() : base(GroupingMode.Apis)
		{
		}

		/// <summary>
		/// Group results by APIs only.
		/// </summary>
		/// <param name="analysisContext">Analysis context.</param>
		/// <param name="diagnosticsWithApis">The diagnostics with APIs.</param>
		/// <param name="projectDirectory">Pathname of the project directory.</param>
		/// <param name="cancellation">Cancellation token.</param>
		/// <returns>
		/// Output API results grouped by API grouping mode only.
		/// </returns>
		public override IEnumerable<ReportGroup> GetApiGroups(AppAnalysisContext analysisContext, DiagnosticsWithBannedApis diagnosticsWithApis,
															  string? projectDirectory, CancellationToken cancellation)
		{

		}
	}
}
