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
	/// Results grouper by types and/or API grouping modes.
	/// </summary>
	internal class GroupByTypesAndAPIs : GroupByAPIsOrNoGrouping
	{
		public GroupByTypesAndAPIs(GroupingMode grouping) : base(grouping) 
		{
		}

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
			var namespacesAndOtherApis = diagnosticsWithApis.ToLookup(d => d.BannedApi.Kind == ApiKind.Namespace);
			var namespacesApis = namespacesAndOtherApis[true];
			var otherApis = namespacesAndOtherApis[false];

			ReportGroup? namespacesSection = GetNamespaceDiagnosticsGroupForTypesOnlyGrouping(analysisContext, namespacesApis.ToList(),
																							  diagnosticsWithApis.DistinctApisCalculator, projectDirectory);
			if (namespacesSection != null)
				yield return namespacesSection;

			cancellation.ThrowIfCancellationRequested();
			var groupedByTypes = otherApis.GroupBy(d => d.BannedApi.FullTypeName)
										  .OrderBy(diagnosticsByTypes => diagnosticsByTypes.Key);

			foreach (var typeDiagnostics in groupedByTypes)
			{
				cancellation.ThrowIfCancellationRequested();
				ReportGroup? typeGroup = GetTypeDiagnosticGroup(typeDiagnostics.Key, analysisContext, typeDiagnostics.ToList(),
																diagnosticsWithApis.UsedBannedTypes, diagnosticsWithApis.DistinctApisCalculator, projectDirectory);
				if (typeGroup != null)
					yield return typeGroup;
			}
		}

		private ReportGroup? GetNamespaceDiagnosticsGroupForTypesOnlyGrouping(AppAnalysisContext analysisContext, 
																			  List<(Diagnostic Diagnostic, Api BannedApi)> diagnostics,
																			  UsedDistinctApisCalculator usedDistinctApisCalculator, string? projectDirectory)
		{
			if (diagnostics.Count == 0)
				return null;

			var distinctApis = usedDistinctApisCalculator.GetAllUsedApis(diagnostics);
			int distinctApisCount = distinctApis.Count();

			var namespacesGroups = GetGroupsAfterNamespaceAndTypeGroupingProcessed(analysisContext, usedDistinctApisCalculator, diagnostics, projectDirectory);
			var namespacesSectionGroup = new ReportGroup
			{
				GroupTitle = new Title("Namespaces", TitleKind.Namespace),
				TotalErrorCount = diagnostics.Count,
				DistinctApisCount = distinctApisCount,
				ChildrenGroups = namespacesGroups.NullIfEmpty()
			};

			return namespacesSectionGroup;
		}

		protected ReportGroup? GetTypeDiagnosticGroup(string typeName, AppAnalysisContext analysisContext, List<(Diagnostic Diagnostic, Api BannedApi)> diagnostics,
													  HashSet<string> usedBannedTypes, UsedDistinctApisCalculator usedDistinctApisCalculator, string? projectDirectory)
		{
			if (!analysisContext.Grouping.HasGrouping(GroupingMode.Apis) && analysisContext.ReportMode == ReportMode.UsedAPIsWithUsages)
			{
				var usedDistinctApis = usedDistinctApisCalculator.GetAllUsedApis(diagnostics);
				int distinctApisCount = usedDistinctApis.Count();

				var flatApiLines = GetFlatApiUsagesLines(diagnostics, projectDirectory, analysisContext).ToList();
				var flatTypeGroup = new ReportGroup
				{
					GroupTitle = new Title(typeName, TitleKind.Type),
					TotalErrorCount = flatApiLines.Count,
					DistinctApisCount = distinctApisCount,
					Lines = flatApiLines
				};

				return flatTypeGroup;
			}

			bool isUsed = usedBannedTypes.Contains(typeName);
			var typeDiagnostics = diagnostics.Where(d => d.BannedApi.Kind == ApiKind.Type).ToList();
			IReadOnlyCollection<Line>? typeUsages = null;

			if (isUsed)
			{
				if (analysisContext.ReportMode == ReportMode.UsedAPIsOnly)
				{
					if (!analysisContext.ShowMembersOfUsedType)
					{
						return new ReportGroup
						{
							GroupTitle = new Title(typeName, TitleKind.Type),
							TotalErrorCount = diagnostics.Count,
							DistinctApisCount = 1
						};
					}
				}
				else
				{
					var sortedTypeUsages = typeDiagnostics.OrderBy(d => d.Diagnostic.Location.SourceTree?.FilePath ?? string.Empty)
														  .Select(d => d.Diagnostic);
					typeUsages = GetApiUsagesLines(sortedTypeUsages, projectDirectory, analysisContext).ToList(capacity: typeDiagnostics.Count);
				}
			}

			var typeMembers = diagnostics.Where(d => d.BannedApi.Kind != ApiKind.Type)
										 .ToList(capacity: diagnostics.Count - (typeUsages?.Count ?? 0));

			if (typeMembers.Count == 0)
			{
				if (typeDiagnostics.Count == 0)
					return null;

				if (analysisContext.ReportMode == ReportMode.UsedAPIsOnly)
				{
					if (!isUsed)
						return null;

					var typeOnlyGroup = new ReportGroup
					{
						GroupTitle = new Title(typeName, TitleKind.Type),
						TotalErrorCount = typeDiagnostics.Count,
						DistinctApisCount = 1
					};

					return typeOnlyGroup;
				}
			}

			IReadOnlyCollection<ReportGroup>? typeGroups = typeMembers.Count > 0
				? GetGroupsAfterNamespaceAndTypeGroupingProcessed(analysisContext, usedDistinctApisCalculator, typeMembers, projectDirectory)
				: null;

			var distinctApis = usedDistinctApisCalculator.GetAllUsedApis(diagnostics);
			var typeGroup = new ReportGroup
			{
				GroupTitle = new Title(typeName, TitleKind.Type),
				TotalErrorCount = diagnostics.Count,
				DistinctApisCount = distinctApis.Count(),

				ChildrenTitle = !typeGroups.IsNullOrEmpty()
									? new Title("Members", TitleKind.Members)
									: null,
				ChildrenGroups = typeGroups.NullIfEmpty(),

				LinesTitle = !typeUsages.IsNullOrEmpty()
									? new Title("Usages", TitleKind.Usages)
									: null,
				Lines = typeUsages.NullIfEmpty()
			};

			return typeGroup;
		}
	}
}
