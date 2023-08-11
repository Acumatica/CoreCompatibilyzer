using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Runner.Output.Data;
using CoreCompatibilyzer.Utils.Common;

using Serilog;

namespace CoreCompatibilyzer.Runner.Output.PlainText
{
	/// <summary>
	/// The base class for the report outputter in the plain text format.
	/// </summary>
	internal class PlainTextReportOutputterFile : PlainTextReportOutputterBase
	{
		private StreamWriter? _streamWriter;

		public override void OutputReport(Report report, AppAnalysisContext analysisContext, CancellationToken cancellation)
		{
			if (analysisContext.OutputFileName.IsNullOrWhiteSpace())
				return;

			try
			{
				_streamWriter = GetStreamWriter(analysisContext.OutputFileName);
				base.OutputReport(report, analysisContext, cancellation);
			}
			finally
			{
				_streamWriter?.Dispose();
				_streamWriter = null;
			}
		}

		protected override void WriteTitle(in Title? title, int depth, int itemsCount)
		{
			if (title == null)
				return;

			string padding = GetPadding(depth);
			string titleWithPadding = $"{padding}{title.Value.Text}(Count = {itemsCount}):";
			WriteLine(titleWithPadding);
		}

		protected override void WriteLine(in Line line, int depth)
		{
			if (line.Spans.IsDefaultOrEmpty)
			{
				WriteLine();
				return;
			}

			string padding = GetPadding(depth);

			if (line.Spans.Length == 2)
			{
				var (fullApiName, location) = (line.Spans[0].ToString(), line.Spans[1].ToString());
				Console.WriteLine($"{padding}{fullApiName}: {location}");
			}
			else
				WriteLine(padding + line.ToString());
		}

		protected override void WriteLine() => _streamWriter?.WriteLine();

		protected override void WriteLine(string text)
		{
			if (text.IsNullOrWhiteSpace())
				_streamWriter?.WriteLine();
			else
				_streamWriter?.WriteLine(text);
		}

		private StreamWriter GetStreamWriter(string outputFileName)
		{
			try
			{
				FileStream outputFileStream = File.OpenWrite(outputFileName);
				return new StreamWriter(outputFileStream);
			}
			catch (Exception e)
			{
				Log.Error(e, "Failed to open the output file {OutputFileName}", outputFileName);
				throw;
			}
		}
	}
}