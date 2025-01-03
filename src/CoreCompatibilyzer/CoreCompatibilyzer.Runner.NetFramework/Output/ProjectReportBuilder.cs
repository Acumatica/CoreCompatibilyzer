﻿using System;
using System.Collections.Generic;
using System.IO;
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
    /// The project report builder's default implementation.
    /// </summary>
    internal class ProjectReportBuilder : IProjectReportBuilder
	{
		public ProjectReport BuildReport(DiagnosticsWithBannedApis diagnosticsWithApis, AppAnalysisContext analysisContext, Project project, CancellationToken cancellation)
		{
			diagnosticsWithApis.ThrowIfNull(nameof(diagnosticsWithApis));
			project.ThrowIfNull(nameof(project));

			cancellation.ThrowIfCancellationRequested();

			string? projectDirectory   = GetProjectDirectory(project);
			
			var mainReportGroup = GetMainReportGroupFromAllDiagnostics(diagnosticsWithApis, analysisContext, projectDirectory, cancellation);
			var report = new ProjectReport(project.Name)
			{
				TotalErrorCount   = diagnosticsWithApis.TotalDiagnosticsCount,
				DistinctApisCount = diagnosticsWithApis.UsedDistinctApis.Count,
				ReportDetails     = mainReportGroup,
			};

			return report;
		}

		private string? GetProjectDirectory(Project project)
		{
			if (project.FilePath.IsNullOrWhiteSpace())
				return null;

			string projectFile = Path.GetFullPath(project.FilePath.Trim());
			string projectDirectory = Path.GetDirectoryName(projectFile);
			return projectDirectory;
		}

		protected virtual ReportGroup GetMainReportGroupFromAllDiagnostics(DiagnosticsWithBannedApis diagnosticsWithApis, AppAnalysisContext analysisContext,
																		   string? projectDirectory, CancellationToken cancellation)
		{
			var bannedApisGroups	  		  = GetAllReportGroups(diagnosticsWithApis, analysisContext, projectDirectory, cancellation).ToList();
			var sortedUnrecognizedDiagnostics = GetLinesForUnrecognizedDiagnostics(diagnosticsWithApis);
			int recognizedErrorsCount 		  = diagnosticsWithApis.TotalDiagnosticsCount - diagnosticsWithApis.UnrecognizedDiagnostics.Count;

			var mainApiGroup = new ReportGroup()
			{
				TotalErrorCount   = recognizedErrorsCount,
				DistinctApisCount = diagnosticsWithApis.UsedDistinctApis.Count,

				ChildrenTitle 	  = new Title("Found APIs", TitleKind.AllApis),
				ChildrenGroups 	  = bannedApisGroups.NullIfEmpty(),
				LinesTitle		  = sortedUnrecognizedDiagnostics.Count > 0
										? new Title("Unrecognized diagnostics", TitleKind.NotSpecified)
										: null,
				Lines			  = sortedUnrecognizedDiagnostics.NullIfEmpty(),
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

            if (analysisContext.Grouping.HasGrouping(GroupingMode.Files))
            {
                return GetApiGroupsGroupedBySourceFiles(analysisContext, diagnosticsWithApis, projectDirectory, cancellation);
            }
            if (analysisContext.Grouping.HasGrouping(GroupingMode.Namespaces))
			{
				return GetApiGroupsGroupedByNamespaces(analysisContext, diagnosticsWithApis, projectDirectory, cancellation);
			}
			if (analysisContext.Grouping.HasGrouping(GroupingMode.Types))
			{
				return GetApiGroupsGroupedOnlyByTypes(analysisContext, diagnosticsWithApis, projectDirectory, cancellation);
			}

			var sortedFlatDiagnostics = diagnosticsWithApis.OrderBy(d => d.BannedApi.FullName);
			var flattenedApiGroups = 
				GetGroupsAfterNamespaceAndTypeGroupingProcessed(analysisContext, diagnosticsWithApis.DistinctApisCalculator, sortedFlatDiagnostics, projectDirectory);

			return flattenedApiGroups;
		}

		private IEnumerable<ReportGroup> GetApiGroupsGroupedByNamespaces(AppAnalysisContext analysisContext, DiagnosticsWithBannedApis diagnosticsWithApis,
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

		private IEnumerable<ReportGroup> GetApiGroupsGroupedOnlyByTypes(AppAnalysisContext analysisContext, DiagnosticsWithBannedApis diagnosticsWithApis,
																		string? projectDirectory, CancellationToken cancellation)
		{
			var namespacesAndOtherApis = diagnosticsWithApis.ToLookup(d => d.BannedApi.Kind == ApiKind.Namespace);
			var namespacesApis 		   = namespacesAndOtherApis[true];
			var otherApis 			   = namespacesAndOtherApis[false];

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

		private ReportGroup? GetApiGroupForNamespaceDiagnostics(string @namespace, AppAnalysisContext analysisContext,
															    List<(Diagnostic Diagnostic, Api BannedApi)> diagnostics, HashSet<string> usedBannedTypes, 
															    HashSet<string> usedNamespaces, UsedDistinctApisCalculator usedDistinctApisCalculator, 
																string? projectDirectory, CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();

			bool groupByApis  = analysisContext.Grouping.HasGrouping(GroupingMode.Apis);
			bool groupByTypes = analysisContext.Grouping.HasGrouping(GroupingMode.Types);

			if (!groupByApis && !groupByTypes && analysisContext.ReportMode == ReportMode.UsedAPIsWithUsages)
			{
				var distinctApisForFlatUsagesGroup = usedDistinctApisCalculator.GetAllUsedApis(diagnostics);
				int distinctApisForFlatUsagesCount = distinctApisForFlatUsagesGroup.Count();

				var flatApiLines = GetFlatApiUsagesLines(diagnostics, projectDirectory, analysisContext).ToList();
				var flatNamespaceGroup = new ReportGroup
				{
					GroupTitle 		  = new Title(@namespace, TitleKind.Namespace),
					TotalErrorCount   = flatApiLines.Count,
					DistinctApisCount = distinctApisForFlatUsagesCount,
					Lines			  = flatApiLines.NullIfEmpty()
				};

				return flatNamespaceGroup;
			}

			IReadOnlyCollection<Line>? namespaceUsages = null;
			bool isUsed = usedNamespaces.Contains(@namespace);
			var namespaceDiagnostics = diagnostics.Where(d => d.BannedApi.Kind == ApiKind.Namespace).ToList();

			if (isUsed && analysisContext.ReportMode == ReportMode.UsedAPIsWithUsages)
			{
				if (groupByApis)
				{
					var sortedNamespaceUsages = namespaceDiagnostics.OrderBy(d => d.Diagnostic.Location.SourceTree?.FilePath ?? string.Empty)
																	.Select(d => d.Diagnostic);
					namespaceUsages = GetApiUsagesLines(sortedNamespaceUsages, projectDirectory, analysisContext).ToList(capacity: namespaceDiagnostics.Count);
				}
				else
					namespaceUsages = GetFlatApiUsagesLines(namespaceDiagnostics, projectDirectory, analysisContext).ToList(capacity: namespaceDiagnostics.Count);
			}

			cancellation.ThrowIfCancellationRequested();
			var namespaceMembers = diagnostics.Where(d => d.BannedApi.Kind != ApiKind.Namespace).ToList();

			if (namespaceMembers.Count == 0)
			{
				if (namespaceDiagnostics.Count == 0)
					return null;

				if (analysisContext.ReportMode == ReportMode.UsedAPIsOnly)
				{
					if (!isUsed)
						return null;

					var namespaceOnlyGroup = new ReportGroup
					{
						GroupTitle 		  = new Title(@namespace, TitleKind.Namespace),
						TotalErrorCount   = namespaceDiagnostics.Count,
						DistinctApisCount = 1
					};

					return namespaceOnlyGroup;
				}
			}
			
			IReadOnlyCollection<ReportGroup>? namespaceGroups = null;

			if (groupByTypes)
			{
				namespaceGroups = GetTypeGroupsForNamespaceMembers(namespaceMembers, analysisContext, usedBannedTypes, usedDistinctApisCalculator,
																   projectDirectory, cancellation)
																  .ToList();
			}
			else
			{
				namespaceGroups = GetGroupsAfterNamespaceAndTypeGroupingProcessed(analysisContext, usedDistinctApisCalculator,
																				  namespaceMembers, projectDirectory);
			}

			var distinctApis	  = usedDistinctApisCalculator.GetAllUsedApis(diagnostics);
			int distinctApisCount = distinctApis.Count();

			var namespaceGroup = new ReportGroup
			{
				GroupTitle 		  = new Title(@namespace, TitleKind.Namespace),
				TotalErrorCount   = diagnostics.Count,
				DistinctApisCount = distinctApisCount,

				ChildrenTitle	  = !namespaceGroups.IsNullOrEmpty() 
										? new Title( "Members", TitleKind.Members) 
										: null,
				ChildrenGroups    = namespaceGroups.NullIfEmpty(),
				LinesTitle		  = !namespaceUsages.IsNullOrEmpty() 
										? new Title("Usages", TitleKind.Usages) 
										: null,
				Lines			  = namespaceUsages.NullIfEmpty()
			};

			return namespaceGroup;
		}

		private IEnumerable<ReportGroup> GetTypeGroupsForNamespaceMembers(List<(Diagnostic Diagnostic, Api BannedApi)> namespaceMembers, AppAnalysisContext analysisContext,
																		  HashSet<string> usedBannedTypes, UsedDistinctApisCalculator usedDistinctApisCalculator,
																		  string? projectDirectory,  CancellationToken cancellation)
		{
			var groupedByTypes = namespaceMembers.GroupBy(d => d.BannedApi.FullTypeName)
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

		private ReportGroup? GetTypeDiagnosticGroup(string typeName, AppAnalysisContext analysisContext, List<(Diagnostic Diagnostic, Api BannedApi)> diagnostics,
													HashSet<string> usedBannedTypes, UsedDistinctApisCalculator usedDistinctApisCalculator, string? projectDirectory)
		{
			if (!analysisContext.Grouping.HasGrouping(GroupingMode.Apis) && analysisContext.ReportMode == ReportMode.UsedAPIsWithUsages)
			{
				var usedDistinctApis  = usedDistinctApisCalculator.GetAllUsedApis(diagnostics);
				int distinctApisCount = usedDistinctApis.Count();

				var flatApiLines  = GetFlatApiUsagesLines(diagnostics, projectDirectory, analysisContext).ToList();
				var flatTypeGroup = new ReportGroup
				{
					GroupTitle		  = new Title(typeName, TitleKind.Type),
					TotalErrorCount   = flatApiLines.Count,
					DistinctApisCount = distinctApisCount,
					Lines 			  = flatApiLines
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
							GroupTitle 		  = new Title(typeName, TitleKind.Type),
							TotalErrorCount   = diagnostics.Count,
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
						GroupTitle 		  = new Title(typeName, TitleKind.Type),
						TotalErrorCount   = typeDiagnostics.Count,
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
				GroupTitle 		  = new Title(typeName, TitleKind.Type),
				TotalErrorCount   = diagnostics.Count,
				DistinctApisCount = distinctApis.Count(),

				ChildrenTitle 	  = !typeGroups.IsNullOrEmpty()
									? new Title("Members", TitleKind.Members)
									: null,
				ChildrenGroups 	  = typeGroups.NullIfEmpty(),

				LinesTitle 		  = !typeUsages.IsNullOrEmpty()
									? new Title("Usages", TitleKind.Usages)
									: null,
				Lines 			  = typeUsages.NullIfEmpty()
			};

			return typeGroup;
		}

		private ReportGroup? GetNamespaceDiagnosticsGroupForTypesOnlyGrouping(AppAnalysisContext analysisContext, List<(Diagnostic Diagnostic, Api BannedApi)> diagnostics,
																			  UsedDistinctApisCalculator usedDistinctApisCalculator, string? projectDirectory)
		{
			if (diagnostics.Count == 0)
				return null;

			var distinctApis	  = usedDistinctApisCalculator.GetAllUsedApis(diagnostics);
			int distinctApisCount = distinctApis.Count();

			var namespacesGroups	   = GetGroupsAfterNamespaceAndTypeGroupingProcessed(analysisContext, usedDistinctApisCalculator, diagnostics, projectDirectory);
			var namespacesSectionGroup = new ReportGroup
			{
				GroupTitle 		  = new Title("Namespaces", TitleKind.Namespace),
				TotalErrorCount   = diagnostics.Count,
				DistinctApisCount = distinctApisCount,
				ChildrenGroups 	  = namespacesGroups.NullIfEmpty()
			};
			
			return namespacesSectionGroup;
		}

		protected IReadOnlyCollection<ReportGroup> GetGroupsAfterNamespaceAndTypeGroupingProcessed(AppAnalysisContext analysisContext, UsedDistinctApisCalculator usedDistinctApisCalculator,
																								   IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> unsortedDiagnostics, 
																								   string? projectDirectory)
		{
			if (analysisContext.ReportMode == ReportMode.UsedAPIsOnly)
			{
				var allDistinctApis	   = usedDistinctApisCalculator.GetAllUsedApis(unsortedDiagnostics);
				var sortedDistinctApis = allDistinctApis.OrderBy(api => api.FullName)
														.Select(api => api.FullName);

				var lines = sortedDistinctApis.Select(line => new Line(line)).ToList(); 
				var usedApisGroup = new ReportGroup
				{
					TotalErrorCount   = lines.Count,
					DistinctApisCount = lines.Count,
					Lines 			  = lines.NullIfEmpty() 
				};

				return new[] { usedApisGroup };
			}
			else if (analysisContext.Grouping.HasGrouping(GroupingMode.Apis))
				return GetApiUsagesGroupsGroupedByApi(unsortedDiagnostics, usedDistinctApisCalculator, projectDirectory, analysisContext).ToList();
			else
			{
				var allDistinctApis   = usedDistinctApisCalculator.GetAllUsedApis(unsortedDiagnostics);
				int distinctApisCount = allDistinctApis.Count();

				var flatApiUsageLines = GetFlatApiUsagesLines(unsortedDiagnostics, projectDirectory, analysisContext).ToList();

				var flatApiUsageGroup = new ReportGroup
				{
					TotalErrorCount   = flatApiUsageLines.Count,
					DistinctApisCount = distinctApisCount,
					Lines 			  = flatApiUsageLines.NullIfEmpty()
				};

				return new[] { flatApiUsageGroup }; 
			}
		}

		protected IEnumerable<ReportGroup> GetApiGroupsGroupedBySourceFiles(AppAnalysisContext analysisContext, DiagnosticsWithBannedApis diagnosticsWithApis,
		                                                                    string? projectDirectory, CancellationToken cancellation)
		{
			var diagnosticsGroupedByApi = diagnosticsWithApis.GroupBy(d => d.Diagnostic.Location.SourceTree?.FilePath).OrderBy(d => d.Key);
			foreach (var diagnosticsByApiGroup in diagnosticsGroupedByApi)
			{
				cancellation.ThrowIfCancellationRequested();

				var    diagnosticsByApi = diagnosticsByApiGroup.ToList();
				string apiName          = diagnosticsByApiGroup.Key ?? string.Empty;
				var    distinctApis     =  diagnosticsWithApis.DistinctApisCalculator.GetAllUsedApis(diagnosticsByApi);
				var    apiDiagnostics   = diagnosticsByApi.OrderBy(d => d.Diagnostic.Location.SourceTree?.FilePath ?? string.Empty);
				var    usagesLines      = GetFileApiUsagesLines(apiDiagnostics, projectDirectory, analysisContext).ToList();
				var apiGroup = new ReportGroup
				{
					GroupTitle        = new Title(apiName, TitleKind.Api),
					TotalErrorCount   = usagesLines.Count,
					DistinctApisCount = distinctApis.Count(),
					LinesTitle        = new Title("Usages", TitleKind.Usages),
					Lines             = usagesLines.NullIfEmpty()
				};

				yield return apiGroup;
			}
		}


		private IEnumerable<ReportGroup> GetApiUsagesGroupsGroupedByApi(IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> unsortedDiagnostics,
																		UsedDistinctApisCalculator usedDistinctApisCalculator,
																		string? projectDirectory, AppAnalysisContext analysisContext)
		{
			var diagnosticsGroupedByApi = unsortedDiagnostics.GroupBy(d => d.BannedApi.FullName)
															 .OrderBy(d => d.Key);
			foreach (var diagnosticsByApiGroup in diagnosticsGroupedByApi)
			{
				var diagnosticsByApi = diagnosticsByApiGroup.ToList();
				string apiName		 = diagnosticsByApiGroup.Key;
				var distinctApis	 = usedDistinctApisCalculator.GetAllUsedApis(diagnosticsByApi);
				var apiDiagnostics 	 = diagnosticsByApi.Select(d => d.Diagnostic)
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

		private IEnumerable<Line> GetFileApiUsagesLines(IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> sortedDiagnostics, string? projectDirectory, AppAnalysisContext analysisContext)
		{
			return sortedDiagnostics.Select(d => GetFileApiUsagesLine(d.Diagnostic, d.BannedApi, projectDirectory, analysisContext));
		}

		private IEnumerable<Line> GetApiUsagesLines(IEnumerable<Diagnostic> sortedDiagnostics, string? projectDirectory,
													AppAnalysisContext analysisContext) =>
			sortedDiagnostics.Select(diagnostic => GetApiUsageLine(diagnostic, projectDirectory, analysisContext));

		private Line GetFileApiUsagesLine(Diagnostic diagnostic, Api bannedApi, string? projectDirectory, AppAnalysisContext analysisContext)
		{
			var span = diagnostic.Location.GetMappedLineSpan().Span;

			return new Line(bannedApi.FullName, span.ToString());
		}


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