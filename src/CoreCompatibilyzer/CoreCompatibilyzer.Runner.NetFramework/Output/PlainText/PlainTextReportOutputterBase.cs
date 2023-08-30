using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Runner.Output.Data;
using CoreCompatibilyzer.Utils.Common;

using Serilog;

namespace CoreCompatibilyzer.Runner.Output.PlainText
{
	/// <summary>
	/// The base class for the report outputter in the plain text format.
	/// </summary>
	internal abstract class PlainTextReportOutputterBase : IReportOutputter
	{
		public abstract void Dispose();

		public virtual void OutputReport(CodeSourceReport codeSourceReport, AppAnalysisContext analysisContext, CancellationToken cancellation)
		{
			codeSourceReport.ThrowIfNull(nameof(codeSourceReport));
			cancellation.ThrowIfCancellationRequested();

			WriteLine($"{codeSourceReport.CodeSourceName} - Total Errors Count: {codeSourceReport.TotalErrorCount}");

			if (!analysisContext.IncludeAllDistinctApis || codeSourceReport.DistinctApis?.Count is null or 0)
				WriteLine($"{codeSourceReport.CodeSourceName} - Distinct APIs Count: {codeSourceReport.DistinctApisCount}");

			if (codeSourceReport.TotalErrorCount == 0)
				return;

			if (analysisContext.IncludeAllDistinctApis && codeSourceReport.DistinctApis?.Count > 0)
				OutputDistinctApis(codeSourceReport.DistinctApis);

			foreach (ProjectReport projectReport in codeSourceReport.ProjectReports)
			{
				OutputReport(projectReport, analysisContext, cancellation);

				if (projectReport.IsEmptyReport())
					WriteLine();
			}
		}

		private void OutputDistinctApis(IReadOnlyCollection<Line> distinctApis)
		{
			WriteLine();
			WriteDistinctApisTitle("Distinct APIs", depth: 0, distinctApis.Count);

			foreach (Line api in distinctApis)
				WriteLine(api, depth: 1);

			WriteLine();
		}

		public virtual void OutputReport(ProjectReport projectReport, AppAnalysisContext analysisContext, CancellationToken cancellation)
		{
			projectReport.ThrowIfNull(nameof(projectReport));
			cancellation.ThrowIfCancellationRequested();

			WriteLine($"{projectReport.ProjectName} - Total Errors Count: {projectReport.TotalErrorCount}");
			WriteLine($"{projectReport.ProjectName} - Distinct APIs Count: {projectReport.DistinctApisCount}");

			if (projectReport.TotalErrorCount == 0)
				return;
			
			if (projectReport.ReportDetails != null)
			{
				OutputApiGroup(projectReport.ReportDetails, depth: 0, cancellation, recursionDepth: 0);
			}
		}

		protected virtual void OutputApiGroup(ReportGroup reportGroup, int depth, CancellationToken cancellation, int recursionDepth)
		{
			cancellation.ThrowIfCancellationRequested();

			const int MaxRecursionDepth = 100;

			if (recursionDepth > MaxRecursionDepth)
			{
				Log.Error("Max recursion depth reached. The program execution most likely resulted in the stack overflow");
				return;
			}

			bool hasTitle = false, hasLines = false, hasSubGroups = false;
			
			if (reportGroup.GroupTitle.HasValue)
			{
				hasTitle = true;
				WriteTitle(reportGroup.GroupTitle.Value, depth, reportGroup.TotalErrorCount, reportGroup.DistinctApisCount, reportGroup.HasContent);
			}

			cancellation.ThrowIfCancellationRequested();

			if (reportGroup.Lines?.Count > 0)
			{
				hasLines = true;
				int linesDepth;

				if (reportGroup.LinesTitle.HasValue)
				{
					WriteTitle(reportGroup.LinesTitle.Value, depth + 1, reportGroup.Lines.Count, reportGroup.DistinctApisCount, reportGroup.HasContent);
					linesDepth = depth + 2;
				}
				else
					linesDepth = depth + 1;

				foreach (Line line in reportGroup.Lines)
				{
					cancellation.ThrowIfCancellationRequested();
					WriteLine(line, linesDepth);
				}
			}

			cancellation.ThrowIfCancellationRequested();

			if (reportGroup.ChildrenGroups?.Count > 0)
			{
				hasSubGroups = true;
				int groupDepth;

				if (reportGroup.ChildrenTitle.HasValue)
				{
					int totalSubGroupErrors = reportGroup.ChildrenGroups.Sum(group => group.TotalErrorCount);
					WriteTitle(reportGroup.ChildrenTitle.Value, depth + 1, totalSubGroupErrors, reportGroup.DistinctApisCount, reportGroup.HasContent);
					groupDepth = depth + 2;
				}
				else
					groupDepth = depth + 1;

				foreach (ReportGroup childGroup in reportGroup.ChildrenGroups)
				{
					cancellation.ThrowIfCancellationRequested();
					OutputApiGroup(childGroup, groupDepth, cancellation, recursionDepth + 1);
				}
			}

			if (!hasSubGroups && (hasLines || hasTitle))
				WriteLine();
		}

		protected abstract void WriteDistinctApisTitle(string titleText, int depth, int itemsCount);

		protected abstract void WriteTitle(in Title? title, int depth, int itemsCount, int distinctApisCount, bool hasContent);

		protected void WriteLine<T>(T obj)
		{
			if (obj is null)
				WriteLine();
			else
				WriteLine(obj.ToString());
		}

		protected abstract void WriteLine();

		protected abstract void WriteLine(string text);

		protected abstract void WriteLine(in Line line, int depth);

		protected string GetPadding(int depth)
		{
			const int paddingMultiplier = 4;
			string padding = depth <= 0
				? string.Empty
				: new string(' ', depth * paddingMultiplier);

			return string.Intern(padding);
		}
	}
}