using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;

using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Runner.Output.Data;
using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.Runner.Output.Json
{
	/// <summary>
	/// JSON report outputter base class.
	/// </summary>
	internal abstract class JsonReportOutputterBase : IReportOutputter
	{
		public abstract void Dispose();

		public virtual void OutputReport(ProjectReport report, AppAnalysisContext analysisContext, CancellationToken cancellation)
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

			cancellation.ThrowIfCancellationRequested();
			OutputReportText(serializedReport);
		}

		protected abstract void OutputReportText(string serializedReport);
	}
}