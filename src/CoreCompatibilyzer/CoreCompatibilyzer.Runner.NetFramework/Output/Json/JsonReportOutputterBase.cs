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

		public virtual void OutputReport(CodeSourceReport codeSourceReport, AppAnalysisContext analysisContext, CancellationToken cancellation)
		{
			codeSourceReport.ThrowIfNull(nameof(codeSourceReport));
			analysisContext.ThrowIfNull(nameof(analysisContext));
			cancellation.ThrowIfCancellationRequested();

			var options = GetJsonSerializerOptions();
			string serializedReport = JsonSerializer.Serialize(codeSourceReport, options);

			cancellation.ThrowIfCancellationRequested();
			OutputReportText(serializedReport);
		}

		public virtual void OutputReport(ProjectReport projectReport, AppAnalysisContext analysisContext, CancellationToken cancellation)
		{
			projectReport.ThrowIfNull(nameof(projectReport));
			analysisContext.ThrowIfNull(nameof(analysisContext));
			cancellation.ThrowIfCancellationRequested();

			var options = GetJsonSerializerOptions();
			string serializedReport = JsonSerializer.Serialize(projectReport, options);

			cancellation.ThrowIfCancellationRequested();
			OutputReportText(serializedReport);
		}

		protected virtual JsonSerializerOptions GetJsonSerializerOptions() =>
			new JsonSerializerOptions
			{
				WriteIndented = true,
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			};

		protected abstract void OutputReportText(string serializedReport);
	}
}