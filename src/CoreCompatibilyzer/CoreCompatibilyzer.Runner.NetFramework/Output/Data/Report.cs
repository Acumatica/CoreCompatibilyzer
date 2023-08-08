using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CoreCompatibilyzer.Runner.Output.Data
{
	internal class Report
	{
		public required int TotalErrorCount { get; init; }

		public required ReportGroup ReportDetails { get; init; }
	}
}
