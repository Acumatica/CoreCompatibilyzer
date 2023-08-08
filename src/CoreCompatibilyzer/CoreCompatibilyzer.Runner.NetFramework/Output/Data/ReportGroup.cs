using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CoreCompatibilyzer.Runner.Output.Data
{
	internal class ReportGroup
	{
		public required string GroupTitle { get; init; }

		public required int TotalErrorCount { get; init; }

		public string? LinesTitle { get; init; }

		public IReadOnlyCollection<OutputLine>? Lines { get; init; }

		public string? ChildrenTitle { get; init; }

		public IReadOnlyCollection<ReportGroup>? ChildrenGroups { get; init; }

		public required int Depth { get; init; }
	}
}
