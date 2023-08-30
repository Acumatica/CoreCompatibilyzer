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
		private readonly StreamWriter _streamWriter;
		private bool _disposed;

		public PlainTextReportOutputterFile(string outputFile)
		{
			outputFile.ThrowIfNullOrWhiteSpace(nameof(outputFile));

			DeleteExistingFile(outputFile);
			_streamWriter = GetStreamWriter(outputFile);
		}

		public sealed override void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				_streamWriter.Dispose();
			}
		}

		public sealed override void OutputReport(CodeSourceReport codeSourceReport, AppAnalysisContext analysisContext, CancellationToken cancellation)
		{
			if (_disposed)
				throw new ObjectDisposedException(objectName: GetType().FullName);

			base.OutputReport(codeSourceReport, analysisContext, cancellation);
		}

		public sealed override void OutputReport(ProjectReport projectReport, AppAnalysisContext analysisContext, CancellationToken cancellation)
		{
			if (_disposed)
				throw new ObjectDisposedException(objectName: GetType().FullName);
			else if (analysisContext.OutputFileName.IsNullOrWhiteSpace())
				return;

			base.OutputReport(projectReport, analysisContext, cancellation);
		}

		protected override void WriteDistinctApisTitle(string titleText, int depth, int itemsCount)
		{
			if (titleText == null)
				return;

			string padding 			= GetPadding(depth);
			string suffix 			= itemsCount > 0 ? ":" : string.Empty;
			string titleWithPadding = $"{padding}{titleText}(Count: {itemsCount}){suffix}";

			WriteLine(titleWithPadding);
		}

		protected override void WriteTitle(in Title? title, int depth, int itemsCount, int distinctApisCount, bool hasContent)
		{
			if (title == null)
				return;

			string padding = GetPadding(depth);
			string suffix  = hasContent ? ":" : string.Empty;
			string titleWithPadding = title.Value.Kind == TitleKind.Usages
				? $"{padding}{title.Value.Text}{suffix}"
				: $"{padding}{title.Value.Text}(Count: {itemsCount}, Distinct APIs: {distinctApisCount}){suffix}";

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
				WriteLine($"{padding}{fullApiName}: {location}");
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

		private void DeleteExistingFile(string outputFile)
		{
			try
			{
				if (File.Exists(outputFile))
					File.Delete(outputFile);
			}
			catch (Exception e)
			{
				Log.Error(e, "Failed to delete the existing output file {OutputFile}", outputFile);
				throw;
			}
		}

		private StreamWriter GetStreamWriter(string outputFile)
		{
			try
			{
				FileStream outputFileStream = File.OpenWrite(outputFile);
				return new StreamWriter(outputFileStream);
			}
			catch (Exception e)
			{
				Log.Error(e, "Failed to open the output file {OutputFile}", outputFile);
				throw;
			}
		}
	}
}