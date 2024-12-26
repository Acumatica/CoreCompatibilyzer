using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using CoreCompatibilyzer.ApiData.Model;
using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Runner.Output.Data;
using CoreCompatibilyzer.Utils.Common;

using Microsoft.CodeAnalysis;

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

		protected IEnumerable<Line> GetFlatApiUsagesLines(IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> unsortedDiagnostics,
														  string? projectDirectory, AppAnalysisContext analysisContext)
		{
			var sortedApisWithLocations = unsortedDiagnostics.Select(d => (FullApiName: d.BannedApi.FullName,
																		   Location: GetPrettyLocation(d.Diagnostic, projectDirectory, analysisContext)))
															 .OrderBy(apiWithLocation => apiWithLocation.FullApiName)
															 .ThenBy(apiWithLocation => apiWithLocation.Location)
															 .Select(apiWithLocation => new Line(apiWithLocation.FullApiName, apiWithLocation.Location));
			return sortedApisWithLocations;
		}

		protected IEnumerable<Line> GetApiUsagesLines(IEnumerable<Diagnostic> sortedDiagnostics, string? projectDirectory,
													  AppAnalysisContext analysisContext) =>
			sortedDiagnostics.Select(diagnostic => GetApiUsageLine(diagnostic, projectDirectory, analysisContext));

		protected Line GetApiUsageLine(Diagnostic diagnostic, string? projectDirectory, AppAnalysisContext analysisContext)
		{
			var prettyLocation = GetPrettyLocation(diagnostic, projectDirectory, analysisContext);
			return new Line(prettyLocation);
		}

		protected string GetPrettyLocation(Diagnostic diagnostic, string? projectDirectory, AppAnalysisContext analysisContext)
		{
			string prettyLocation = diagnostic.Location.GetMappedLineSpan().ToString();

			if (analysisContext.OutputAbsolutePathsToUsages || projectDirectory.IsNullOrWhiteSpace())
				return prettyLocation;

			StringComparison stringComparison = analysisContext.IsRunningOnLinux
				? StringComparison.Ordinal
				: StringComparison.OrdinalIgnoreCase;

			if (!prettyLocation.StartsWith(projectDirectory, stringComparison))
				return prettyLocation;

			string relativeLocation = "." + prettyLocation.Substring(projectDirectory.Length);
			return relativeLocation;
		}
	}
}
