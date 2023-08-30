using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.Runner.Output.Data
{
	internal class CodeSourceReport
	{
		public string CodeSourceName { get; }

		public int TotalErrorCount { get; }

		public int DistinctApisCount => DistinctApis?.Count ?? 0;

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public IReadOnlyCollection<Line>? DistinctApis { get; init; }

		public IReadOnlyCollection<ProjectReport> ProjectReports { get; }

        public CodeSourceReport(string codeSourceName, IEnumerable<Line>? distinctApis, IEnumerable<ProjectReport> projectReports)
        {
			CodeSourceName  = codeSourceName.ThrowIfNullOrWhiteSpace(nameof(codeSourceName));
			DistinctApis	= distinctApis?.ToList();
			ProjectReports  = projectReports.ThrowIfNullOrEmpty(nameof(projectReports)).ToList();
			TotalErrorCount = ProjectReports.Sum(report => report.TotalErrorCount);
		}
    }
}
