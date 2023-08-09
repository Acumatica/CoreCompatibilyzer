using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.Runner.Output.Data
{
	internal readonly struct Line : IEquatable<Line>, IComparable<Line>
	{
		public ImmutableArray<LineSpan> Spans { get; }

		public Line(string line)
		{
			var span = new LineSpan(line.ThrowIfNull(nameof(line)));
			Spans	 = ImmutableArray.Create(span);
		}

		public Line(string part1, string part2)
		{
			var span1 = new LineSpan(part1.ThrowIfNull(nameof(part1)));
			var span2 = new LineSpan(part2.ThrowIfNull(nameof(part2)));
			Spans = ImmutableArray.Create(span1, span2);
		}

		public Line(IReadOnlyCollection<string> spans)
        {
			Spans = spans.ThrowIfNull(nameof(spans))
						 .Select(s => new LineSpan(s)).ToImmutableArray();
        }

		public override string ToString()
		{
			if (Spans.Length == 1)
				return Spans[0].ToString();
			else if (Spans.Length == 2)
				return $"{Spans[0].ToString()} {Spans[1].ToString()}";
			else if (Spans.Length > 2)
				return string.Join(" ", Spans);
			else
				return string.Empty;
		}

		public override bool Equals(object obj) =>
			obj is Line other && Equals(other);

		public bool Equals(Line other)
		{
			switch (Spans.Length)
			{
				case 0:
					return other.Spans.Length == 0;
				case 1 
				when other.Spans.Length == 1:
					return string.Equals(Spans[0].Text, other.Spans[0].Text, StringComparison.Ordinal);

				default:
					string line		 = ToString();
					string otherLine = other.ToString();

					return string.Equals(line, otherLine, StringComparison.Ordinal);
			}
		}

		public int CompareTo(Line other)
		{
			switch (Spans.Length)
			{
				case 0:
					return other.Spans.Length == 0
						? 0
						: -1;
				case 1
				when other.Spans.Length == 1:
					return Spans[0].CompareTo(other.Spans[0]);

				default:
					if (other.Spans.Length == 0)
						return 1;

					string line		 = ToString();
					string otherLine = other.ToString();

					return string.Compare(line, otherLine, StringComparison.Ordinal);
			}
		}

		public override int GetHashCode()
		{
			if (Spans.Length == 1)
				return Spans[0].GetHashCode();
			else if (Spans.Length > 1)
			{
				int hash = 17;

				unchecked
				{
					foreach (var span in Spans)
						hash = 23 * hash + span.GetHashCode();
				}

				return hash;
			}
			else
				return 0;
		}
	}
}
