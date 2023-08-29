﻿using System;
using System.Collections.Immutable;
using System.Threading;

using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Runner.Output.Data;

using Microsoft.CodeAnalysis;

namespace CoreCompatibilyzer.Runner.Output
{
	/// <summary>
	/// Interface for the project report builder.
	/// </summary>
	internal interface IProjectReportBuilder
	{
		/// <summary>
		/// Builds the report from hte diagnostics.
		/// </summary>
		/// <param name="diagnostics">The diagnostics.</param>
		/// <param name="analysisContext">The analysis context.</param>
		/// <param name="project">The project.</param>
		/// <param name="cancellation">Cancellation token.</param>
		ProjectReport BuildReport(ImmutableArray<Diagnostic> diagnostics, AppAnalysisContext analysisContext, Project project,
								  CancellationToken cancellation);
	}
}