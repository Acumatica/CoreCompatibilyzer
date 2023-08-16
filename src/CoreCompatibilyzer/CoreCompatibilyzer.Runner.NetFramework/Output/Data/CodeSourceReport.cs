using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.Runner.Output.Data
{
	internal class CodeSourceReport
	{
		public string CodeSourceName { get; }

		public int TotalErrorCount { get; }

		public IReadOnlyCollection<ProjectReport> ProjectReports { get; }

        public CodeSourceReport(string codeSourceName, IEnumerable<ProjectReport> projectReports)
        {
			CodeSourceName  = codeSourceName.ThrowIfNullOrWhiteSpace(nameof(codeSourceName));
			ProjectReports  = projectReports.ThrowIfNullOrEmpty(nameof(projectReports)).ToList();
			TotalErrorCount = ProjectReports.Sum(report => report.TotalErrorCount);
		}
    }
}
