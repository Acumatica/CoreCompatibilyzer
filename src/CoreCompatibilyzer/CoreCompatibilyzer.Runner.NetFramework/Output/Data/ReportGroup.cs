using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CoreCompatibilyzer.Runner.Output.Data
{
	internal class ReportGroup
	{
		public string? GroupTitle { get; init; }

		public required int TotalErrorCount { get; init; }

		public string? LinesTitle { get; init; }

		public IReadOnlyCollection<Line>? Lines { get; init; }

		public string? ChildrenTitle { get; init; }

		public IReadOnlyCollection<ReportGroup>? ChildrenGroups { get; init; }

		public required int Depth { get; init; }
	}
}
