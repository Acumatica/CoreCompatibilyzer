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
		public void OutputReport(Report report, AppAnalysisContext analysisContext, CancellationToken cancellation)
		{
			report.ThrowIfNull(nameof(report));

			if (report.TotalErrorCount == 0)
				return;
			
			WriteLine($"Total errors count: {report.TotalErrorCount}");
			cancellation.ThrowIfCancellationRequested();

			if (report.ReportDetails != null)
			{
				OutputApiGroup(report.ReportDetails, depth: 0, cancellation, recursionDepth: 0);
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
				WriteTitle(reportGroup.GroupTitle.Value, depth, reportGroup.TotalErrorCount);
			}

			cancellation.ThrowIfCancellationRequested();

			if (reportGroup.Lines?.Count > 0)
			{
				hasLines = true;
				int linesDepth;

				if (reportGroup.LinesTitle.HasValue)
				{
					WriteTitle(reportGroup.LinesTitle.Value, depth + 1, reportGroup.Lines.Count);
					linesDepth = depth + 2;
				}
				else
					linesDepth = depth + 1;

				foreach (Line line in reportGroup.Lines)
				{
					cancellation.ThrowIfCancellationRequested();
					WriteLine(line, linesDepth);
				}

				WriteLine();
			}

			cancellation.ThrowIfCancellationRequested();

			if (reportGroup.ChildrenGroups?.Count > 0)
			{
				hasSubGroups = true;
				int groupDepth;

				if (reportGroup.ChildrenTitle.HasValue)
				{
					WriteTitle(reportGroup.ChildrenTitle.Value, depth + 1, reportGroup.ChildrenGroups.Count);
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

			if (hasTitle || hasLines || hasSubGroups)
				WriteLine();
		}

		protected abstract void WriteTitle(in Title? title, int depth, int itemsCount);

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