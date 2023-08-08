using System;
using System.Collections.Immutable;
using System.Threading;

using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Runner.Output.Data;

namespace CoreCompatibilyzer.Runner.Output
{
	/// <summary>
	/// Interface for the report outputter.
	/// </summary>
	internal interface IReportOutputter
	{
		/// <summary>
		/// Outputs report.
		/// </summary>
		/// <param name="report">The report.</param>
		/// <param name="analysisContext">The analysis context.</param>
		/// <param name="cancellation">Cancellation token.</param>
		void OutputReport(Report report, AppAnalysisContext analysisContext, CancellationToken cancellation);
	}
}
