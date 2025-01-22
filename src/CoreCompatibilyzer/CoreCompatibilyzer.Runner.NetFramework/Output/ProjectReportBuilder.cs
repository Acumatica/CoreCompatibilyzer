using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

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

			var linesGrouper = GetLinesGrouper(analysisContext.Grouping);
			var apiGroups	 = linesGrouper.GetApiGroups(analysisContext, diagnosticsWithApis, projectDirectory, cancellation);
			return apiGroups;
		}

		protected virtual IGroupLines GetLinesGrouper(GroupingMode groupingMode)
		{
			if (groupingMode.HasGrouping(GroupingMode.Files))
			{
				return new GroupByAnyGroupingCombination(groupingMode);
			}
			else if (groupingMode.HasGrouping(GroupingMode.Namespaces))
			{
				return new GroupByNamespacesTypesAndApis(groupingMode);
			}
			else if (groupingMode.HasGrouping(GroupingMode.Types)) 
			{
				return new GroupByTypesAndAPIs(groupingMode);
			}
			else
				return new GroupByAPIsOrNoGrouping(groupingMode);
		}
	}
}