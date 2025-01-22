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
	/// Results grouper by combination of namespace, types, and API grouping modes.
	/// </summary>
	internal class GroupByNamespacesTypesAndApis : GroupByTypesAndAPIs
	{
		public GroupByNamespacesTypesAndApis(GroupingMode grouping) : base(grouping)
		{
		}

		/// <summary>
		/// Group results by combination of namespace, types, and APIs.
		/// </summary>
		/// <param name="analysisContext">Analysis context.</param>
		/// <param name="diagnosticsWithApis">The diagnostics with APIs.</param>
		/// <param name="projectDirectory">Pathname of the project directory.</param>
		/// <param name="cancellation">Cancellation token.</param>
		/// <returns>
		/// Output API results grouped by combination of namespace, types, and APIs specified by grouping modes.
		/// </returns>
		public override IEnumerable<ReportGroup> GetApiGroups(AppAnalysisContext analysisContext, DiagnosticsWithBannedApis diagnosticsWithApis,
															  string? projectDirectory, CancellationToken cancellation) =>
			GetApiGroupsByNamespaces(analysisContext, diagnosticsWithApis, projectDirectory, cancellation);

		protected IEnumerable<ReportGroup> GetApiGroupsByNamespaces(AppAnalysisContext analysisContext, DiagnosticsWithBannedApis diagnosticsWithApis,
																	string? projectDirectory, CancellationToken cancellation)
		{
			var groupedByNamespaces = diagnosticsWithApis.GroupBy(d => d.BannedApi.Namespace)
														 .OrderBy(diagnosticsByNamespaces => diagnosticsByNamespaces.Key);

			foreach (var namespaceDiagnostics in groupedByNamespaces)
			{
				cancellation.ThrowIfCancellationRequested();
				var namespaceGroup = GetApiGroupForNamespaceDiagnostics(namespaceDiagnostics.Key, analysisContext, namespaceDiagnostics.ToList(),
																		diagnosticsWithApis.UsedBannedTypes, diagnosticsWithApis.UsedNamespaces,
																		diagnosticsWithApis.DistinctApisCalculator, projectDirectory, cancellation);
				if (namespaceGroup != null)
					yield return namespaceGroup;
			}
		}

		private ReportGroup? GetApiGroupForNamespaceDiagnostics(string @namespace, AppAnalysisContext analysisContext,
																List<(Diagnostic Diagnostic, Api BannedApi)> diagnostics, HashSet<string> usedBannedTypes,
																HashSet<string> usedNamespaces, UsedDistinctApisCalculator usedDistinctApisCalculator,
																string? projectDirectory, CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();

			bool groupByApis = analysisContext.Grouping.HasGrouping(GroupingMode.Apis);
			bool groupByTypes = analysisContext.Grouping.HasGrouping(GroupingMode.Types);

			if (!groupByApis && !groupByTypes && analysisContext.ReportMode == ReportMode.UsedAPIsWithUsages)
			{
				return CreateGroupByNamespaceOnlyWithApiUsages(@namespace, analysisContext, diagnostics, usedDistinctApisCalculator, projectDirectory);
			}

			bool isNamespaceUsed 		= usedNamespaces.Contains(@namespace);
			var diagnosticsForNamespace = diagnostics.Where(d => d.BannedApi.Kind == ApiKind.Namespace).ToList();
			var namespaceUsages 		= GetNamespaceUsages(analysisContext, projectDirectory, groupByApis, isNamespaceUsed, diagnosticsForNamespace);

			cancellation.ThrowIfCancellationRequested();
			var diagnosticsForNamespaceMembers = diagnostics.Where(d => d.BannedApi.Kind != ApiKind.Namespace).ToList();

			if (diagnosticsForNamespaceMembers.Count == 0)
			{
				if (diagnosticsForNamespace.Count == 0)
					return null;

				if (analysisContext.ReportMode == ReportMode.UsedAPIsOnly)
				{
					if (!isNamespaceUsed)
						return null;

					var namespaceOnlyGroup = new ReportGroup
					{
						GroupTitle = new Title(@namespace, TitleKind.Namespace),
						TotalErrorCount = diagnosticsForNamespace.Count,
						DistinctApisCount = 1
					};

					return namespaceOnlyGroup;
				}
			}

			IReadOnlyCollection<ReportGroup>? namespaceGroups =
				GetGroupsForNamespaceMembers(analysisContext, usedBannedTypes, usedDistinctApisCalculator, projectDirectory, groupByTypes,
											 diagnosticsForNamespaceMembers, cancellation);

			var distinctApis = usedDistinctApisCalculator.GetAllUsedApis(diagnostics);
			int distinctApisCount = distinctApis.Count();
			ReportGroup namespaceGroup = CreateGroupForNamespaceWithNamespaceAndNamespaceMembersUsages(@namespace, diagnostics, namespaceUsages, 
																										namespaceGroups, distinctApisCount);
			return namespaceGroup;
		}

		private ReportGroup CreateGroupByNamespaceOnlyWithApiUsages(string @namespace, AppAnalysisContext analysisContext, 
																   List<(Diagnostic Diagnostic, Api BannedApi)> diagnostics, 
																   UsedDistinctApisCalculator usedDistinctApisCalculator, string? projectDirectory)
		{
			var distinctApisForFlatUsagesGroup = usedDistinctApisCalculator.GetAllUsedApis(diagnostics);
			int distinctApisForFlatUsagesCount = distinctApisForFlatUsagesGroup.Count();

			var flatApiLines = GetFlatApiUsagesLines(diagnostics, projectDirectory, analysisContext).ToList();
			var flatNamespaceGroup = new ReportGroup
			{
				GroupTitle = new Title(@namespace, TitleKind.Namespace),
				TotalErrorCount = flatApiLines.Count,
				DistinctApisCount = distinctApisForFlatUsagesCount,
				Lines = flatApiLines.NullIfEmpty()
			};

			return flatNamespaceGroup;
		}

		private IReadOnlyCollection<Line>? GetNamespaceUsages(AppAnalysisContext analysisContext, string? projectDirectory, bool groupByApis, 
															  bool isNamespaceUsed, List<(Diagnostic Diagnostic, Api BannedApi)> diagnosticsForNamespace)
		{
			if (isNamespaceUsed && analysisContext.ReportMode == ReportMode.UsedAPIsWithUsages)
			{
				if (groupByApis)
				{
					var sortedNamespaceUsages = diagnosticsForNamespace.OrderBy(d => d.Diagnostic.Location.SourceTree?.FilePath ?? string.Empty)
																	   .Select(d => d.Diagnostic);
					return GetApiUsagesLines(sortedNamespaceUsages, projectDirectory, analysisContext).ToList(capacity: diagnosticsForNamespace.Count);
				}
				else
					return GetFlatApiUsagesLines(diagnosticsForNamespace, projectDirectory, analysisContext).ToList(capacity: diagnosticsForNamespace.Count);
			}

			return null;
		}

		private IReadOnlyCollection<ReportGroup> GetGroupsForNamespaceMembers(AppAnalysisContext analysisContext, HashSet<string> usedBannedTypes,
																			  UsedDistinctApisCalculator usedDistinctApisCalculator, string? projectDirectory,
																			  bool groupByTypes, List<(Diagnostic Diagnostic, Api BannedApi)> diagnosticsForNamespaceMembers,
																			  CancellationToken cancellation)
		{
			if (groupByTypes)
			{
				return GetTypeGroupsForNamespaceMembers(diagnosticsForNamespaceMembers, analysisContext, usedBannedTypes, usedDistinctApisCalculator,
														projectDirectory, cancellation)
													   .ToList();
			}
			else
			{
				return GetGroupsAfterNamespaceAndTypeGroupingProcessed(analysisContext, usedDistinctApisCalculator,
																	   diagnosticsForNamespaceMembers, projectDirectory);
			}
		}

		private IEnumerable<ReportGroup> GetTypeGroupsForNamespaceMembers(List<(Diagnostic Diagnostic, Api BannedApi)> diagnosticsForNamespaceMembers, 
																		  AppAnalysisContext analysisContext,
																		  HashSet<string> usedBannedTypes, UsedDistinctApisCalculator usedDistinctApisCalculator,
																		  string? projectDirectory, CancellationToken cancellation)
		{
			var groupedByTypes = diagnosticsForNamespaceMembers.GroupBy(d => d.BannedApi.FullTypeName)
															   .OrderBy(diagnosticsByTypes => diagnosticsByTypes.Key);
			foreach (var typeDiagnostics in groupedByTypes)
			{
				cancellation.ThrowIfCancellationRequested();

				string typeName = typeDiagnostics.Key;
				var typeGroup = GetTypeDiagnosticGroup(typeName, analysisContext, typeDiagnostics.ToList(),
													   usedBannedTypes, usedDistinctApisCalculator, projectDirectory);
				if (typeGroup != null)
					yield return typeGroup;
			}
		}

		private static ReportGroup CreateGroupForNamespaceWithNamespaceAndNamespaceMembersUsages(string @namespace, 
																				List<(Diagnostic Diagnostic, Api BannedApi)> diagnostics, 
																				IReadOnlyCollection<Line>? namespaceUsages, 
																				IReadOnlyCollection<ReportGroup> namespaceGroups, int distinctApisCount)
		{
			return new ReportGroup
			{
				GroupTitle 		  = new Title(@namespace, TitleKind.Namespace),
				TotalErrorCount   = diagnostics.Count,
				DistinctApisCount = distinctApisCount,

				ChildrenTitle = !namespaceGroups.IsNullOrEmpty()
									? new Title("Members", TitleKind.Members)
									: null,
				ChildrenGroups = namespaceGroups.NullIfEmpty(),
				LinesTitle = !namespaceUsages.IsNullOrEmpty()
									? new Title("Usages", TitleKind.Usages)
									: null,
				Lines = namespaceUsages.NullIfEmpty()
			};
		}
	}
}
