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
			var diagnosticsGroupedByApi = diagnosticsWithApis.GroupBy(d => d.Diagnostic.Location.SourceTree?.FilePath.NullIfWhiteSpace() ?? string.Empty)
															 .OrderBy(d => d.Key);

			foreach (var diagnosticsByApiGroup in diagnosticsGroupedByApi)
			{
				cancellation.ThrowIfCancellationRequested();

				var diagnosticsByApi = diagnosticsByApiGroup.ToList();
				string apiName = diagnosticsByApiGroup.Key ?? string.Empty;
				var distinctApis = diagnosticsWithApis.DistinctApisCalculator.GetAllUsedApis(diagnosticsByApi);
				var apiDiagnostics = diagnosticsByApi.OrderBy(d => d.Diagnostic.Location.SourceTree?.FilePath ?? string.Empty);
				var usagesLines = GetFileApiUsagesLines(apiDiagnostics, projectDirectory, analysisContext).ToList();
				var apiGroup = new ReportGroup
				{
					GroupTitle = new Title(apiName, TitleKind.Api),
					TotalErrorCount = usagesLines.Count,
					DistinctApisCount = distinctApis.Count(),
					LinesTitle = new Title("Usages", TitleKind.Usages),
					Lines = usagesLines.NullIfEmpty()
				};

				yield return apiGroup;
			}
		}

		private IEnumerable<Line> GetFileApiUsagesLines(IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> sortedDiagnostics, string? projectDirectory, AppAnalysisContext analysisContext)
		{
			return sortedDiagnostics.Select(d => GetFileApiUsagesLine(d.Diagnostic, d.BannedApi, projectDirectory, analysisContext));
		}

		private Line GetFileApiUsagesLine(Diagnostic diagnostic, Api bannedApi, string? projectDirectory, AppAnalysisContext analysisContext)
		{
			var span = diagnostic.Location.GetMappedLineSpan().Span;

			return new Line(bannedApi.FullName, span.ToString());
		}
	}
}
