using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.Runner.Output.Data
{
	internal class ProjectReport
	{
		public string ProjectName { get; }

		public required int TotalErrorCount { get; init; }

		public required int DistinctApisCount { get; init; }

		public required ReportGroup ReportDetails { get; init; }

        public ProjectReport(string projectName)
        {
            ProjectName = projectName.ThrowIfNullOrWhiteSpace(nameof(projectName));
        }

		public bool IsEmptyReport() => ReportDetails == null ||
			(ReportDetails.GroupTitle == null &&
			 ReportDetails.Lines?.Count is null or 0 && ReportDetails.LinesTitle == null &&
			 ReportDetails.ChildrenGroups?.Count is null or 0 && ReportDetails.ChildrenTitle == null);
    }
}
