using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

using CoreCompatibilyzer.Runner.Output.Data;

using Serilog;

namespace CoreCompatibilyzer.Runner.Output.Json
{
	/// <summary>
	/// JSON line converter.
	/// </summary>
	internal class LineConverter : JsonConverter<Line>
	{
		public override Line Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
			throw new NotSupportedException();

		public override void Write(Utf8JsonWriter writer, Line line, JsonSerializerOptions options)
		{
			switch (line.Spans.Length)
			{
				case 0:
					return;

				case 2:
					var (fullApiName, location) = (line.Spans[0].ToString(), line.Spans[1].ToString());

					writer.WriteStringValue($"{fullApiName}: {location}");
					return;

				default:
					string lineStr = line.ToString();

					if (!string.IsNullOrWhiteSpace(lineStr))
						writer.WriteStringValue(lineStr);

					return;
			}
		}
	}
}