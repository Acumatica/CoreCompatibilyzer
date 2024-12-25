using System;
using System.Collections.Generic;
using System.Threading;

using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Runner.Output.Data;

namespace CoreCompatibilyzer.Runner.Output
{
	/// <summary>
	/// Base class to group report lines.
	/// </summary>
	internal abstract class GroupLinesBase : IGroupLines
	{
		/// <summary>
		/// Gets the required output results grouping.
		/// </summary>
		public GroupingMode Grouping { get; }

		protected GroupLinesBase(GroupingMode grouping)
		{
			Grouping = grouping;
		}

		/// <summary>
		/// Get API groups
		/// </summary>
		/// <param name="analysisContext">Analysis context.</param>
		/// <param name="diagnosticsWithApis">The diagnostics with APIs.</param>
		/// <param name="projectDirectory">Pathname of the project directory.</param>
		/// <param name="cancellation">Cancellation token.</param>
		/// <returns>
		/// Output API results grouped by <see cref="Grouping"/>.
		/// </returns>
		public abstract IEnumerable<ReportGroup> GetApiGroups(AppAnalysisContext analysisContext, DiagnosticsWithBannedApis diagnosticsWithApis,
															  string? projectDirectory, CancellationToken cancellation);
	}
}
