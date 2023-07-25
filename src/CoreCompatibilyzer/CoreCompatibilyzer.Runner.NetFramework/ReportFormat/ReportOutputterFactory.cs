using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

using CoreCompatibilyzer.Runner.Input;
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

			Stream stream = GetStream(analysisContext);
			return new PlainTextReportOutputter(stream);
		}

		private Stream GetStream(AppAnalysisContext analysisContext) 
		{
			if (analysisContext.OutputFileName.IsNullOrWhiteSpace())
			{
				try
				{
					Stream consoleStream = Console.OpenStandardOutput();
					return consoleStream;
				}
				catch (Exception e)
				{
					Log.Error(e, "Failed to obtain the console output stream");
					throw;
				}
			}

			string outputFileName = analysisContext.OutputFileName.Trim();

			try
			{
				FileStream outputFileStream = File.OpenWrite(outputFileName);
				return outputFileStream;
			}
			catch (Exception e)
			{
				Log.Error(e, "Failed to open the output file {OutputFileName}", outputFileName);
				throw;
			}
		}
	}
}