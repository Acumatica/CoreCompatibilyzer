using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
	/// The report builder's default implementation.
	/// </summary>
	internal class ReportBuilder : IReportBuilder
	{
		public Report BuildReport(ImmutableArray<Diagnostic> diagnostics, AppAnalysisContext analysisContext, string? projectDirectory, 
								  CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();

			var diagnosticsWithApis = diagnostics.IsDefaultOrEmpty
				? new DiagnosticsWithBannedApis()
				: new DiagnosticsWithBannedApis(diagnostics);

			cancellation.ThrowIfCancellationRequested();

			var mainReportGroup = GetMainReportGroupFromAllDiagnostics(diagnosticsWithApis, analysisContext, projectDirectory, cancellation);
			var report = new Report
			{
				TotalErrorCount = diagnosticsWithApis.TotalDiagnosticsCount,
				ReportDetails   = mainReportGroup,
			};

			return report;
		}

		protected virtual ReportGroup GetMainReportGroupFromAllDiagnostics(DiagnosticsWithBannedApis diagnosticsWithApis, AppAnalysisContext analysisContext,
																		   string? projectDirectory, CancellationToken cancellation)
		{
			var bannedApisGroups	  		  = GetAllReportGroups(diagnosticsWithApis, analysisContext, projectDirectory, cancellation).ToList();
			var sortedUnrecognizedDiagnostics = GetLinesForUnrecognizedDiagnostics(diagnosticsWithApis);
			int recognizedErrorsCount 		  = diagnosticsWithApis.TotalDiagnosticsCount - diagnosticsWithApis.UnrecognizedDiagnostics.Count;

			var mainApiGroup = new ReportGroup()
			{
				TotalErrorCount = recognizedErrorsCount,
				ChildrenTitle 	= new Title("Found APIs", TitleKind.AllApis),
				ChildrenGroups 	= bannedApisGroups,
				Depth			= 0,
				LinesTitle		= new Title("Unrecognized diagnostics", TitleKind.NotSpecified),
				Lines			= sortedUnrecognizedDiagnostics,
			};

			return mainApiGroup;
		}

		protected virtual IReadOnlyList<Line> GetLinesForUnrecognizedDiagnostics(DiagnosticsWithBannedApis diagnosticsWithApis) =>
			(from diagnostic in diagnosticsWithApis.UnrecognizedDiagnostics
			 orderby (diagnostic.Location.SourceTree?.FilePath ?? string.Empty)
			 select new Line(diagnostic.ToString())
			)
			.ToList(capacity: diagnosticsWithApis.UnrecognizedDiagnostics.Count);

		protected virtual IEnumerable<ReportGroup> GetAllReportGroups(DiagnosticsWithBannedApis diagnosticsWithApis, AppAnalysisContext analysisContext,
																	  string? projectDirectory, CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();

			if (analysisContext.Grouping.HasGrouping(GroupingMode.Namespaces))
			{
				return GetApiGroupsGroupedByNamespaces(analysisContext, diagnosticsWithApis, projectDirectory, cancellation);
			}
			else if (analysisContext.Grouping.HasGrouping(GroupingMode.Types))
			{
				return GetApiGroupsGroupedOnlyByTypes(analysisContext, diagnosticsWithApis, projectDirectory, cancellation);
			}
			else
			{
				var sortedFlatDiagnostics = diagnosticsWithApis.OrderBy(d => d.BannedApi.FullName);
				var flattenedApiGroups = 
					GetGroupsAfterNamespaceAndTypeGroupingProcessed(analysisContext, depth: 1, sortedFlatDiagnostics, 
																		diagnosticsWithApis.UsedBannedTypes, projectDirectory);
				return flattenedApiGroups;
			}
		}

		private IEnumerable<ReportGroup> GetApiGroupsGroupedByNamespaces(AppAnalysisContext analysisContext, DiagnosticsWithBannedApis diagnosticsWithApis,
																		 string? projectDirectory, CancellationToken cancellation)
		{
			var groupedByNamespaces = diagnosticsWithApis.GroupBy(d => d.BannedApi.Namespace)
														 .OrderBy(diagnosticsByNamespaces => diagnosticsByNamespaces.Key);

			foreach (var namespaceDiagnostics in groupedByNamespaces)
			{
				cancellation.ThrowIfCancellationRequested();
				var namespaceGroup = GetApiGroupForNamespaceDiagnostics(namespaceDiagnostics.Key, analysisContext, depth: 0, namespaceDiagnostics.ToList(),
																		diagnosticsWithApis.UsedBannedTypes, diagnosticsWithApis.UsedNamespaces, 
																		projectDirectory, cancellation);
				if (namespaceGroup != null)
					yield return namespaceGroup;
			}
		}

		private IEnumerable<ReportGroup> GetApiGroupsGroupedOnlyByTypes(AppAnalysisContext analysisContext, DiagnosticsWithBannedApis diagnosticsWithApis,
																		string? projectDirectory, CancellationToken cancellation)
		{
			var namespacesAndOtherApis = diagnosticsWithApis.ToLookup(d => d.BannedApi.Kind == ApiKind.Namespace);
			var namespacesApis 		   = namespacesAndOtherApis[true];
			var otherApis 			   = namespacesAndOtherApis[false];

			ReportGroup? namespacesSection = GettNamespaceDiagnosticsGroupForTypesOnlyGrouping(analysisContext, namespacesApis.ToList(), 
																								diagnosticsWithApis.UsedBannedTypes, projectDirectory);
			if (namespacesSection != null)
				yield return namespacesSection;

			cancellation.ThrowIfCancellationRequested();
			var groupedByTypes = otherApis.GroupBy(d => d.BannedApi.FullTypeName)
										  .OrderBy(diagnosticsByTypes => diagnosticsByTypes.Key);

			foreach (var typeDiagnostics in groupedByTypes)
			{
				cancellation.ThrowIfCancellationRequested();
				ReportGroup? typeGroup = GetTypeDiagnosticGroup(typeDiagnostics.Key, analysisContext, depth: 0, typeDiagnostics.ToList(), 
															    diagnosticsWithApis.UsedBannedTypes, projectDirectory);
				if (typeGroup != null)
					yield return typeGroup;
			}
		}

		private ReportGroup? GetApiGroupForNamespaceDiagnostics(string @namespace, AppAnalysisContext analysisContext, int depth,
															    List<(Diagnostic Diagnostic, Api BannedApi)> diagnostics, HashSet<string> usedBannedTypes, 
															    HashSet<string> usedNamespaces, string? projectDirectory, CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();

			bool groupByApis  = analysisContext.Grouping.HasGrouping(GroupingMode.Apis);
			bool groupByTypes = analysisContext.Grouping.HasGrouping(GroupingMode.Types);

			if (!groupByApis && !groupByTypes)
			{
				var flatApiLines = GetFlatApiUsagesLines(diagnostics, projectDirectory, analysisContext).ToList();
				var flatNamespaceGroup = new ReportGroup
				{
					Depth 			= depth,
					GroupTitle 		= new Title(@namespace, TitleKind.Namespace),
					TotalErrorCount = flatApiLines.Count,
					Lines			= flatApiLines
				};

				return flatNamespaceGroup;
			}

			IReadOnlyCollection<Line>? namespaceUsages = null;

			if (usedNamespaces.Contains(@namespace) && analysisContext.ReportMode == ReportMode.UsedAPIsWithUsages)
			{
				var namespaceDiagnostics = diagnostics.Where(d => d.BannedApi.Kind == ApiKind.Namespace);

				if (groupByApis)
				{
					var sortedNamespaceUsages = namespaceDiagnostics.OrderBy(d => d.Diagnostic.Location.SourceTree?.FilePath ?? string.Empty)
																	.Select(d => d.Diagnostic);
					namespaceUsages = GetApiUsagesLines(sortedNamespaceUsages, projectDirectory, analysisContext).ToList();
				}
				else
					namespaceUsages = GetFlatApiUsagesLines(namespaceDiagnostics, projectDirectory, analysisContext).ToList();
			}

			cancellation.ThrowIfCancellationRequested();
			var namespaceMembers = diagnostics.Where(d => d.BannedApi.Kind != ApiKind.Namespace).ToList();

			if (namespaceMembers.Count == 0 && namespaceUsages?.Count is null or 0)
				return null;
			
			IReadOnlyCollection<ReportGroup>? namespaceGroups = null;

			if (groupByTypes)
			{
				namespaceGroups = GetTypeGroupsForNamespaceMembers(namespaceMembers, analysisContext, depth, usedBannedTypes, 
																   projectDirectory, cancellation)
																  .ToList();
			}
			else
			{
				namespaceGroups = GetGroupsAfterNamespaceAndTypeGroupingProcessed(analysisContext, depth + 1, namespaceMembers, 
																				  usedBannedTypes, projectDirectory);
			}

			var namespaceGroup = new ReportGroup
			{
				Depth 			= depth,
				GroupTitle 		= new Title(@namespace, TitleKind.Namespace),
				TotalErrorCount = diagnostics.Count,
				ChildrenTitle	= !namespaceGroups.IsNullOrEmpty() 
									? new Title( "Members", TitleKind.Members) 
									: null,
				ChildrenGroups  = namespaceGroups,
				LinesTitle		= !namespaceUsages.IsNullOrEmpty() 
									? new Title("Usages", TitleKind.Usages) 
									: null,
				Lines			= namespaceUsages
			};

			return namespaceGroup;
		}

		private IEnumerable<ReportGroup> GetTypeGroupsForNamespaceMembers(List<(Diagnostic Diagnostic, Api BannedApi)> namespaceMembers, 
																		  AppAnalysisContext analysisContext, int depth, HashSet<string> usedBannedTypes, 
																		  string? projectDirectory,  CancellationToken cancellation)
		{
			var groupedByTypes = namespaceMembers.GroupBy(d => d.BannedApi.FullTypeName)
												 .OrderBy(diagnosticsByTypes => diagnosticsByTypes.Key);

			foreach (var typeDiagnostics in groupedByTypes)
			{
				cancellation.ThrowIfCancellationRequested();

				string typeName = typeDiagnostics.Key;
				var typeGroup = GetTypeDiagnosticGroup(typeName, analysisContext, depth + 1, typeDiagnostics.ToList(),
													   usedBannedTypes, projectDirectory);
				if (typeGroup != null)
					yield return typeGroup;
			}
		}

		private ReportGroup? GetTypeDiagnosticGroup(string typeName, AppAnalysisContext analysisContext, int depth,
													List<(Diagnostic Diagnostic, Api BannedApi)> diagnostics,
													HashSet<string> usedBannedTypes, string? projectDirectory)
		{
			if (!analysisContext.Grouping.HasGrouping(GroupingMode.Apis))
			{
				var flatApiLines  = GetFlatApiUsagesLines(diagnostics, projectDirectory, analysisContext).ToList();
				var flatTypeGroup = new ReportGroup
				{
					Depth			= depth,
					GroupTitle		= new Title(typeName, TitleKind.Type),
					TotalErrorCount = flatApiLines.Count,
					Lines 			= flatApiLines
				};

				return flatTypeGroup;
			}

			IReadOnlyCollection<Line>? typeUsages = null;

			if (usedBannedTypes.Contains(typeName))
			{
				if (analysisContext.ReportMode == ReportMode.UsedAPIsOnly)
				{
					return new ReportGroup
					{
						Depth 			= depth,
						GroupTitle 		= new Title(typeName, TitleKind.Type),
						TotalErrorCount = diagnostics.Count
					};
				}

				var sortedTypeUsages = from d in diagnostics
									   where d.BannedApi.Kind == ApiKind.Type
									   orderby d.Diagnostic.Location.SourceTree?.FilePath ?? string.Empty
									   select d.Diagnostic;
				typeUsages = GetApiUsagesLines(sortedTypeUsages.ToList(), projectDirectory, analysisContext).ToList();
			}

			var typeMembers = diagnostics.Where(d => d.BannedApi.Kind != ApiKind.Type)
										 .ToList(capacity: diagnostics.Count - (typeUsages?.Count ?? 0));

			if (typeMembers.Count == 0 && typeUsages?.Count is null or 0)
				return null;
			
			IReadOnlyCollection<ReportGroup>? typeGroups = typeMembers.Count > 0
				? GetGroupsAfterNamespaceAndTypeGroupingProcessed(analysisContext, depth + 1, typeMembers, usedBannedTypes, projectDirectory)
				: null;
			
			var typeGroup = new ReportGroup
			{
				Depth 			= depth,
				GroupTitle 		= new Title(typeName, TitleKind.Type),
				TotalErrorCount = diagnostics.Count,

				ChildrenTitle 	= !typeGroups.IsNullOrEmpty()
									? new Title("Members", TitleKind.Members)
									: null,
				ChildrenGroups 	= typeGroups,

				LinesTitle 		= !typeUsages.IsNullOrEmpty()
									? new Title("Usages", TitleKind.Usages)
									: null,
				Lines 			= typeUsages
			};

			return typeGroup;
		}

		private ReportGroup? GettNamespaceDiagnosticsGroupForTypesOnlyGrouping(AppAnalysisContext analysisContext,
																			   List<(Diagnostic Diagnostic, Api BannedApi)> diagnostics,
																			   HashSet<string> usedBannedTypes, string? projectDirectory)
		{
			if (diagnostics.Count == 0)
				return null;

			var namespacesGroups = GetGroupsAfterNamespaceAndTypeGroupingProcessed(analysisContext, depth: 1, diagnostics, 
																				   usedBannedTypes, projectDirectory);
			var namespacesSectionGroup = new ReportGroup
			{
				GroupTitle 		= new Title("Namespaces", TitleKind.Namespace),
				Depth 			= 0,
				TotalErrorCount = diagnostics.Count,
				ChildrenGroups 	= namespacesGroups
			};
			
			return namespacesSectionGroup;
		}

		protected IReadOnlyCollection<ReportGroup> GetGroupsAfterNamespaceAndTypeGroupingProcessed(AppAnalysisContext analysisContext, int depth, 
																							IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> unsortedDiagnostics,
																							HashSet<string> usedBannedTypes, string? projectDirectory)
		{
			if (analysisContext.ReportMode == ReportMode.UsedAPIsOnly)
			{
				var allApis = GetAllUsedApis(analysisContext, unsortedDiagnostics, usedBannedTypes);
				var lines = allApis.Select(line => new Line(line)).ToList(); 
				var usedApisGroup = new ReportGroup
				{
					Depth 			= depth,
					TotalErrorCount = lines.Count,
					Lines 			= lines 
				};

				return new[] { usedApisGroup };
			}
			else if (analysisContext.Grouping.HasGrouping(GroupingMode.Apis))
				return GetApiUsagesGroupsGroupedByApi(depth, unsortedDiagnostics, projectDirectory, analysisContext).ToList();
			else
			{
				var flatApiUsageLines = GetFlatApiUsagesLines(unsortedDiagnostics, projectDirectory, analysisContext).ToList();
				var flatApiUsageGroup = new ReportGroup
				{
					Depth 			= depth,
					TotalErrorCount = flatApiUsageLines.Count,
					Lines 			= flatApiUsageLines
				};

				return new[] { flatApiUsageGroup }; 
			}
		}

		private IEnumerable<ReportGroup> GetApiUsagesGroupsGroupedByApi(int depth, IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> unsortedDiagnostics,
																		string? projectDirectory, AppAnalysisContext analysisContext)
		{
			var diagnosticsGroupedByApi = unsortedDiagnostics.GroupBy(d => d.BannedApi.FullName)
															 .OrderBy(d => d.Key);
			foreach (var diagnosticsByApi in diagnosticsGroupedByApi)
			{
				string apiName = diagnosticsByApi.Key;
				var apiDiagnostics = diagnosticsByApi.Select(d => d.Diagnostic)
													 .OrderBy(d => d.Location.SourceTree?.FilePath ?? string.Empty);
				var usagesLines = GetApiUsagesLines(apiDiagnostics, projectDirectory, analysisContext).ToList();
				var apiGroup = new ReportGroup
				{
					Depth 			= depth,
					GroupTitle 		= new Title(apiName, TitleKind.Api),
					TotalErrorCount = usagesLines.Count,
					LinesTitle 		= new Title("Usages", TitleKind.Usages),
					Lines 			= usagesLines
				};

				yield return apiGroup;
			}
		}

		private IEnumerable<Line> GetFlatApiUsagesLines(IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> unsortedDiagnostics,
														string? projectDirectory, AppAnalysisContext analysisContext)
		{
			var sortedApisWithLocations = unsortedDiagnostics.Select(d => (FullApiName: d.BannedApi.FullName, 
																		   Location: GetPrettyLocation(d.Diagnostic, projectDirectory, analysisContext)))
															 .OrderBy(apiWithLocation => apiWithLocation.FullApiName)
															 .ThenBy(apiWithLocation => apiWithLocation.Location)
															 .Select(apiWithLocation => new Line(apiWithLocation.FullApiName, apiWithLocation.Location));
			return sortedApisWithLocations;
		}

		protected IEnumerable<string> GetAllUsedApis(AppAnalysisContext analysisContext, 
													 IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> unsortedDiagnostics,
													 HashSet<string> usedBannedTypes)
		{
			var sortedUsedApi = unsortedDiagnostics.Select(d => d.BannedApi)
												   .Distinct()
												   .OrderBy(api => api.FullName);
			foreach (Api api in sortedUsedApi)
			{
				switch (api.Kind)
				{
					case ApiKind.Namespace:
					case ApiKind.Type
					when analysisContext.ShowMembersOfUsedType || api.ContainingTypes.IsDefaultOrEmpty || !IsContainingTypeUsedBannedType(api):
						yield return api.FullName;
						continue;

					case ApiKind.Field:
					case ApiKind.Property:
					case ApiKind.Event:
					case ApiKind.Method:
						if (analysisContext.ShowMembersOfUsedType || !usedBannedTypes.Contains(api.FullTypeName))
							yield return api.FullName;

						continue;
				}
			}

			//------------------------------------Local Function------------------------------------------
			bool IsContainingTypeUsedBannedType(Api api)
			{
				string containingTypeName = $"{api.Namespace}";

				for (int i = 0; i < api.ContainingTypes.Length; i++)
				{
					containingTypeName += $".{api.ContainingTypes[i]}";

					if (usedBannedTypes!.Contains(containingTypeName))
						return true;
				}

				return false;
			}
		}

		private IEnumerable<Line> GetApiUsagesLines(IEnumerable<Diagnostic> sortedDiagnostics, string? projectDirectory,
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