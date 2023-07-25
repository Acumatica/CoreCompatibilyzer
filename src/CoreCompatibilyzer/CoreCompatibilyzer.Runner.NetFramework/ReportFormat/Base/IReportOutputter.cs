using System;
using System.Collections.Immutable;
using System.Threading;

using CoreCompatibilyzer.Runner.Input;

using Microsoft.CodeAnalysis;

namespace CoreCompatibilyzer.Runner.Output
{
	/// <summary>
	/// Interface for the report outputters.
	/// </summary>
	internal interface IReportOutputter : IDisposable
	{
		/// <summary>
		/// Output diagnostics.
		/// </summary>
		/// <param name="diagnostics">The diagnostics.</param>
		/// <param name="analysisContext">The analysis context.</param>
		/// <param name="cancellation">Cancellation token.</param>
		void OutputDiagnostics(ImmutableArray<Diagnostic> diagnostics, AppAnalysisContext analysisContext, CancellationToken cancellation);
	}
}
