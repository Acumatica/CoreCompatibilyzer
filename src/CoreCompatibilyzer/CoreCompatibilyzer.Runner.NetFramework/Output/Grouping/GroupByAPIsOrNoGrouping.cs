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
	/// Results grouper by <see cref="GroupingMode.Apis"/> or <see cref="GroupingMode.None"/> grouping modes.
	/// </summary>
	internal class GroupByAPIsOrNoGrouping : GroupLinesBase
	{
		public GroupByAPIsOrNoGrouping(GroupingMode grouping) : base(grouping)
		{
		}

		/// <summary>
		/// Group results by <see cref="GroupingMode.Apis"/> or <see cref="GroupingMode.None"/> grouping modes.
		/// </summary>
		/// <param name="analysisContext">Analysis context.</param>
		/// <param name="diagnosticsWithApis">The diagnostics with APIs.</param>
		/// <param name="projectDirectory">Pathname of the project directory.</param>
		/// <param name="cancellation">Cancellation token.</param>
		/// <returns>
		/// Output API results grouped by <see cref="GroupingMode.Apis"/> or <see cref="GroupingMode.None"/> grouping modes.
		/// </returns>
		public override IEnumerable<ReportGroup> GetApiGroups(AppAnalysisContext analysisContext, DiagnosticsWithBannedApis diagnosticsWithApis,
															  string? projectDirectory, CancellationToken cancellation)
		{
			var sortedFlatDiagnostics = diagnosticsWithApis.OrderBy(d => d.BannedApi.FullName);
			var flattenedApiGroups =
				GetGroupsAfterNamespaceAndTypeGroupingProcessed(analysisContext, diagnosticsWithApis.DistinctApisCalculator, 
																sortedFlatDiagnostics, projectDirectory);
			return flattenedApiGroups;
		}

		protected IReadOnlyCollection<ReportGroup> GetGroupsAfterNamespaceAndTypeGroupingProcessed(AppAnalysisContext analysisContext, 
																								UsedDistinctApisCalculator usedDistinctApisCalculator,
																								IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> unsortedDiagnostics,
																								string? projectDirectory)
		{
			bool isGroupedByApis = analysisContext.Grouping.HasGrouping(GroupingMode.Apis);

			switch (analysisContext.ReportMode)
			{
				case ReportMode.UsedAPIsOnly:
					return CreateAggregatedGroupWithoutUsagesForUsedApi(usedDistinctApisCalculator, unsortedDiagnostics);

				case ReportMode.UsedAPIsWithUsages when isGroupedByApis:
					return GetGroupsForApiUsagesGroupedByApi(unsortedDiagnostics, usedDistinctApisCalculator, projectDirectory, analysisContext).ToList();

				case ReportMode.UsedAPIsWithUsages when !isGroupedByApis:
					return GetGroupForFlattenedAPIsCombinedWithTheirUsages(analysisContext, usedDistinctApisCalculator, unsortedDiagnostics, projectDirectory);

				default:
					throw new NotSupportedException($"Report mode \"{analysisContext.ReportMode}\" is not supported");
			}
		}

		private static IReadOnlyCollection<ReportGroup> CreateAggregatedGroupWithoutUsagesForUsedApi(UsedDistinctApisCalculator usedDistinctApisCalculator, 
																									IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> unsortedDiagnostics)
		{
			var allDistinctApis = usedDistinctApisCalculator.GetAllUsedApis(unsortedDiagnostics);
			var sortedDistinctApis = allDistinctApis.OrderBy(api => api.FullName)
													.Select(api => api.FullName);

			var lines = sortedDistinctApis.Select(line => new Line(line)).ToList();
			var usedApisGroup = new ReportGroup
			{
				TotalErrorCount = lines.Count,
				DistinctApisCount = lines.Count,
				Lines = lines.NullIfEmpty()
			};

			return new[] { usedApisGroup };
		}

		private IEnumerable<ReportGroup> GetGroupsForApiUsagesGroupedByApi(IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> unsortedDiagnostics,
																		   UsedDistinctApisCalculator usedDistinctApisCalculator,
																		   string? projectDirectory, AppAnalysisContext analysisContext)
		{
			var diagnosticsGroupedByApi = unsortedDiagnostics.GroupBy(d => d.BannedApi.FullName)
															 .OrderBy(d => d.Key);

			foreach (var diagnosticsForApiGroup in diagnosticsGroupedByApi)
			{
				var diagnosticsForApi = diagnosticsForApiGroup.ToList();
				string apiName 		  = diagnosticsForApiGroup.Key;
				var distinctApis 	  = usedDistinctApisCalculator.GetAllUsedApis(diagnosticsForApi);
				var apiDiagnostics 	  = diagnosticsForApi.Select(d => d.Diagnostic)
														 .OrderBy(d => d.Location.SourceTree?.FilePath ?? string.Empty);
				var usagesLines = GetApiUsagesLines(apiDiagnostics, projectDirectory, analysisContext).ToList();
				var apiGroup = new ReportGroup
				{
					GroupTitle 		  = new Title(apiName, TitleKind.Api),
					TotalErrorCount   = usagesLines.Count,
					DistinctApisCount = distinctApis.Count(),
					LinesTitle 		  = new Title("Usages", TitleKind.Usages),
					Lines 			  = usagesLines.NullIfEmpty()
				};

				yield return apiGroup;
			}
		}

		private IReadOnlyCollection<ReportGroup> GetGroupForFlattenedAPIsCombinedWithTheirUsages(AppAnalysisContext analysisContext,
																								 UsedDistinctApisCalculator usedDistinctApisCalculator,
																								 IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> unsortedDiagnostics,
																								 string? projectDirectory)
		{
			var allDistinctApis = usedDistinctApisCalculator.GetAllUsedApis(unsortedDiagnostics);
			int distinctApisCount = allDistinctApis.Count();

			var flatApiUsageLines = GetFlatApiUsagesLines(unsortedDiagnostics, projectDirectory, analysisContext).ToList();

			var flatApiUsageGroup = new ReportGroup
			{
				TotalErrorCount = flatApiUsageLines.Count,
				DistinctApisCount = distinctApisCount,
				Lines = flatApiUsageLines.NullIfEmpty()
			};

			return new[] { flatApiUsageGroup };
		}
	}
}
