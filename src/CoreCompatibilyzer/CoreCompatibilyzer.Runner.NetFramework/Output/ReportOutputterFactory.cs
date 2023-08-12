using System;
using System.Collections.Generic;

using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Runner.Output.Json;
using CoreCompatibilyzer.Runner.Output.PlainText;
using CoreCompatibilyzer.Utils.Common;

using Serilog;

namespace CoreCompatibilyzer.Runner.Output
{
	/// <summary>
	/// The standard output formatter.
	/// </summary>
	internal class ReportOutputterFactory : IOutputterFactory
	{
		public IReportOutputter CreateOutputter(AppAnalysisContext analysisContext)
		{
			analysisContext.ThrowIfNull(nameof(analysisContext));

			if (analysisContext.OutputFormat == OutputFormat.PlainText)
			{
				if (analysisContext.OutputFileName.IsNullOrWhiteSpace())
					return new PlainTextReportOutputterConsole();
				else
					return new PlainTextReportOutputterFile();
			}
			else if (analysisContext.OutputFormat == OutputFormat.Json)
			{
				return new JsonReportOutputter();
			}
			else
				throw new NotSupportedException();
		}
	}
}