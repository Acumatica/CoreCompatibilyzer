﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using System.Text.Json.Serialization;

namespace CoreCompatibilyzer.Runner.Output.Data
{
	internal class ReportGroup
	{
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public Title? GroupTitle { get; init; }

		public required int TotalErrorCount { get; init; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public Title? LinesTitle { get; init; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public IReadOnlyCollection<Line>? Lines { get; init; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public Title? ChildrenTitle { get; init; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public IReadOnlyCollection<ReportGroup>? ChildrenGroups { get; init; }
	}
}
