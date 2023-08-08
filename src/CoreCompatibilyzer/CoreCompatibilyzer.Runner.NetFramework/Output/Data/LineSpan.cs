using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.Runner.Output.Data
{
	internal readonly struct LineSpan : IEquatable<LineSpan>, IComparable<LineSpan>
	{
		public string? Text { get; }

        public LineSpan(string text)
        {
			Text = text.ThrowIfNullOrWhiteSpace(nameof(text));
        }

		public override bool Equals(object obj) =>
			obj is LineSpan other && Equals(other);

		public bool Equals(LineSpan other)
		{
			if (Text == null)
				return other.Text == null;
			else
				return Text.Equals(other.Text);
		}

		public int CompareTo(LineSpan other)
		{
			if (Text == null && other.Text == null)
				return 0;
			else if (other.Text == null)
				return 1;
			else if (Text == null)
				return -1;
			else
				return Text.CompareTo(other.Text);
		}

		public override int GetHashCode() => Text?.GetHashCode() ?? 0;

		public override string ToString() => Text ?? string.Empty;
	}
}
