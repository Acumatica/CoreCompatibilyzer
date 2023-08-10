using System;

using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.Runner.Output.Data
{
	internal readonly struct Title
	{
		public TitleKind Kind { get; }

		public string Text { get; }

        public Title(string text, TitleKind titleKind)
        {
			Text = text.ThrowIfNullOrWhiteSpace(nameof(text));
			Kind = titleKind;
        }

		public override string ToString() => $"{Kind.ToString()}: { Text ?? string.Empty }";
	}
}
