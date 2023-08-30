using System;
using System.Threading;

using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Runner.Output.Data;

namespace CoreCompatibilyzer.Runner.Output
{
	/// <summary>
	/// Interface for the report outputter.
	/// </summary>
	internal interface IReportOutputter : IDisposable
	{
		/// <summary>
		/// Outputs the code source report.
		/// </summary>
		/// <param name="codeSourceReport">The code source report.</param>
		/// <param name="analysisContext">The analysis context.</param>
		/// <param name="cancellation">Cancellation token.</param>
		void OutputReport(CodeSourceReport codeSourceReport, AppAnalysisContext analysisContext, CancellationToken cancellation);
	}
}
