using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CoreCompatibilyzer.Runner.Output.Data
{
	internal class ReportGroup
	{
		public Title? GroupTitle { get; init; }

		public required int TotalErrorCount { get; init; }

		public Title? LinesTitle { get; init; }

		public IReadOnlyCollection<Line>? Lines { get; init; }

		public Title? ChildrenTitle { get; init; }

		public IReadOnlyCollection<ReportGroup>? ChildrenGroups { get; init; }
	}
}
