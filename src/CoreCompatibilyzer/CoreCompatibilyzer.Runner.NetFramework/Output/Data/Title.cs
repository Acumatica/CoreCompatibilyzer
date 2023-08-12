using System;
using System.Text.Json.Serialization;

using CoreCompatibilyzer.Runner.Output.Json;
using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.Runner.Output.Data
{
	[JsonConverter(typeof(TitleConverter))]
	internal readonly struct Title
	{
		[JsonIgnore(Condition = JsonIgnoreCondition.Always)]
		public TitleKind Kind { get; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string Text { get; }

        public Title(string text, TitleKind titleKind)
        {
			Text = text.ThrowIfNullOrWhiteSpace(nameof(text));
			Kind = titleKind;
        }

		public override string ToString() => $"{Kind.ToString()}: { Text ?? string.Empty }";
	}
}
