using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Runner.Output.Data;
using CoreCompatibilyzer.Utils.Common;

using Serilog;

namespace CoreCompatibilyzer.Runner.Output.Json
{
	/// <summary>
	/// JSON report outputter.
	/// </summary>
	internal class JsonReportOutputter : IReportOutputter
	{
		private StreamWriter? _streamWriter;

		public void OutputReport(Report report, AppAnalysisContext analysisContext, CancellationToken cancellation)
		{
			report.ThrowIfNull(nameof(report));
			analysisContext.ThrowIfNull(nameof(analysisContext));
			cancellation.ThrowIfCancellationRequested();

			var options = new JsonSerializerOptions
			{
				WriteIndented = true,
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			};

			string serializedReport = JsonSerializer.Serialize(report, options);

			if (analysisContext.OutputFileName.IsNullOrWhiteSpace())
				Console.WriteLine(serializedReport);
			else
				File.WriteAllText(analysisContext.OutputFileName, serializedReport);
		}
	}
}