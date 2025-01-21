using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Runner.Output.Data;
using CoreCompatibilyzer.Utils.Common;

using Microsoft.CodeAnalysis;

namespace CoreCompatibilyzer.Runner.Output
{
	/// <summary>
	/// Results grouper by any combination of file, namespace, types, and API grouping modes.
	/// </summary>
	internal sealed class GroupByAnyGroupingCombination : GroupByNamespacesTypesAndApis
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
			var diagnosticsGroupedByFiles = diagnosticsWithApis.GroupBy(d => d.Diagnostic.Location.SourceTree?.FilePath.NullIfWhiteSpace() ?? string.Empty)
															   .OrderBy(d => d.Key);

			foreach (var diagnosticsByApiGroup in diagnosticsGroupedByFiles)
			{
				cancellation.ThrowIfCancellationRequested();

				var diagnosticsByFile = diagnosticsByApiGroup.ToList();
				string fileName 	  = diagnosticsByApiGroup.Key.NullIfWhiteSpace() ?? "No file";

				var diagnosticsInFileWithApis = new DiagnosticsWithBannedApis(diagnosticsByFile!, analysisContext);
				var fileGroup = GetGroupForFileDiagnostics(analysisContext, diagnosticsInFileWithApis, projectDirectory, fileName, cancellation);

				if (fileGroup != null)
					yield return fileGroup;
			}
		}

		private ReportGroup? GetGroupForFileDiagnostics(AppAnalysisContext analysisContext, DiagnosticsWithBannedApis diagnosticsInFileWithApis, 
														string? projectDirectory, string fileName, CancellationToken cancellation)
		{
			bool groupByNamespaces = analysisContext.Grouping.HasGrouping(GroupingMode.Namespaces);
			bool groupByTypes 	   = analysisContext.Grouping.HasGrouping(GroupingMode.Types);
			bool groupByApis 	   = analysisContext.Grouping.HasGrouping(GroupingMode.Apis);

			if (!groupByNamespaces && !groupByApis && !groupByApis)
				return CreateGroupByFileOnly(fileName, analysisContext, diagnosticsInFileWithApis, projectDirectory);

			var subGroups = GetSubGroups(analysisContext, diagnosticsInFileWithApis, projectDirectory, groupByNamespaces, groupByTypes, cancellation);
			var fileGroup = new ReportGroup
			{
				GroupTitle 		  = new Title(fileName, TitleKind.File),
				TotalErrorCount   = diagnosticsInFileWithApis.Count,
				DistinctApisCount = diagnosticsInFileWithApis.UsedDistinctApis.Count,
				ChildrenGroups 	  = subGroups.NullIfEmpty()
			};

			return fileGroup;
		}

		private List<ReportGroup>? GetSubGroups(AppAnalysisContext analysisContext, DiagnosticsWithBannedApis diagnosticsInFileWithApis, 
												string? projectDirectory, bool groupByNamespaces, bool groupByTypes, CancellationToken cancellation)
		{
			List<ReportGroup>? subGroups;
			if (groupByNamespaces)
			{
				subGroups = GetApiGroupsByNamespaces(analysisContext, diagnosticsInFileWithApis, projectDirectory, cancellation)?.ToList();
			}
			else if (groupByTypes)
			{
				subGroups = GetApiGroupsForTypesAndApisGrouping(analysisContext, diagnosticsInFileWithApis, projectDirectory, cancellation)?.ToList();
			}
			else
			{
				subGroups = GetApiGroupsGroupedByApi(analysisContext, diagnosticsInFileWithApis, projectDirectory, cancellation)?.ToList();
			}

			return subGroups;
		}

		private ReportGroup CreateGroupByFileOnly(string fileName, AppAnalysisContext analysisContext, DiagnosticsWithBannedApis diagnosticsInFileWithApis,
												  string? projectDirectory)
		{
			List<Line> lines;
		
			if (analysisContext.ReportMode == ReportMode.UsedAPIsWithUsages)
			{
				lines = GetFlatApiUsagesLines(diagnosticsInFileWithApis, projectDirectory, analysisContext).ToList(diagnosticsInFileWithApis.Count);
			}
			else
			{
				lines = diagnosticsInFileWithApis.OrderBy(d => d.BannedApi.FullName)
												 .Select(d => new Line(d.BannedApi.FullName))
												 .ToList(diagnosticsInFileWithApis.Count);
			}

			var fileGroup = new ReportGroup
			{
				GroupTitle 		  = new Title(fileName, TitleKind.File),
				TotalErrorCount   = diagnosticsInFileWithApis.Count,
				DistinctApisCount = diagnosticsInFileWithApis.UsedDistinctApis.Count,
				Lines 			  = lines.NullIfEmpty()
			};

			return fileGroup;
		}
	}
}
