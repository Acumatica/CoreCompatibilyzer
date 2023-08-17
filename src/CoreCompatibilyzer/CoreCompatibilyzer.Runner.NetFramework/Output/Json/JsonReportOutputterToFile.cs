using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Runner.Output.Data;
using CoreCompatibilyzer.Utils.Common;

using Serilog;

namespace CoreCompatibilyzer.Runner.Output.Json
{
	/// <summary>
	/// JSON report outputter to file.
	/// </summary>
	internal class JsonReportOutputterToFile : JsonReportOutputterBase
	{
		private readonly StreamWriter _streamWriter;
		private bool _disposed;

		public JsonReportOutputterToFile(string outputFile)
		{
			outputFile.ThrowIfNullOrWhiteSpace(nameof(outputFile));

			DeleteExistingFile(outputFile);
			_streamWriter = GetStreamWriter(outputFile);
		}

		public override void Dispose()
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

		public sealed override void OutputReport(ProjectReport report, AppAnalysisContext analysisContext, CancellationToken cancellation)
		{
			if (_disposed)
				throw new ObjectDisposedException(objectName: GetType().FullName);

			base.OutputReport(report, analysisContext, cancellation);
		}

		protected override void OutputReportText(string serializedReport) =>
			_streamWriter.WriteLine(serializedReport);

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