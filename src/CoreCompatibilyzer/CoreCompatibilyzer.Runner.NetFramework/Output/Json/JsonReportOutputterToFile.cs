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

        public JsonReportOutputterToFile(string outputFileName)
        {
			outputFileName.ThrowIfNullOrWhiteSpace(nameof(outputFileName));

			_streamWriter = GetStreamWriter(outputFileName);
		}

		public override void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				_streamWriter.Dispose();
			}
		}

		public override void OutputReport(Report report, AppAnalysisContext analysisContext, CancellationToken cancellation)
		{
			if (_disposed)
				throw new ObjectDisposedException(objectName: GetType().FullName);

			base.OutputReport(report, analysisContext, cancellation);
		}

		protected override void OutputReportText(string serializedReport) => 
			_streamWriter.WriteLine(serializedReport);

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