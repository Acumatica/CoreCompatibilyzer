using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.Runner.Output.Data
{
	internal class Report
	{
		public string ProjectName { get; }

		public required int TotalErrorCount { get; init; }

		public required ReportGroup ReportDetails { get; init; }

        public Report(string projectName)
        {
            ProjectName = projectName.ThrowIfNullOrWhiteSpace(nameof(projectName));
        }
    }
}
