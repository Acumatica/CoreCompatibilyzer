using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.Runner.Output.Data
{
	internal readonly struct OutputLine : IEquatable<OutputLine>, IComparable<OutputLine>
	{
		public string? Line { get; }

		public int Depth { get; }

        public OutputLine(string line, int depth)
        {
            Line  = line.ThrowIfNullOrWhiteSpace();
			Depth = depth;
        }

		public override bool Equals(object obj) =>
			obj is OutputLine other && Equals(other);

		public bool Equals(OutputLine other)
		{
			if (Line == null)
				return other.Line == null;
			else
				return Line.Equals(other.Line);
		}

		public int CompareTo(OutputLine other)
		{
			if (Line == null && other.Line == null)
				return 0;
			else if (other.Line == null)
				return 1;
			else if (Line == null)
				return -1;
			else
				return Line.CompareTo(other.Line);
		}

		public override int GetHashCode() => Line?.GetHashCode() ?? 0;
	}
}
