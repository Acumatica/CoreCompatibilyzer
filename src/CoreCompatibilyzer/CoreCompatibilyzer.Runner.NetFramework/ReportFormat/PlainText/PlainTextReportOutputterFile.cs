using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;

using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Utils.Common;

using Microsoft.CodeAnalysis;

using Serilog;

namespace CoreCompatibilyzer.Runner.NetFramework.ReportFormat.PlainText
{
	/// <summary>
	/// The base class for the report outputter in the plain text format.
	/// </summary>
	internal class PlainTextReportOutputterFile : PlainTextReportOutputterBase
	{
		private StreamWriter? _streamWriter;

		public override void OutputDiagnostics(ImmutableArray<Diagnostic> diagnostics, AppAnalysisContext analysisContext, CancellationToken cancellation)
		{
			if (analysisContext.OutputFileName.IsNullOrWhiteSpace())
				return;

			try
			{
				_streamWriter = GetStreamWriter(analysisContext.OutputFileName);
				base.OutputDiagnostics(diagnostics, analysisContext, cancellation);
			}
			finally
			{
				_streamWriter?.Dispose();
				_streamWriter = null;
			}
		}

		protected override void WriteAllApisTitle(string allApisTitle) =>
			WriteLine(allApisTitle);

		protected override void WriteApiTitle(string apiTitle) =>
			WriteLine(apiTitle);

		protected override void WriteNamespaceTitle(string namespaceTitle) =>
			WriteLine(namespaceTitle);

		protected override void WriteTypeMembersTitle(string typeMembersTitle) =>
			WriteLine(typeMembersTitle);

		protected override void WriteTypeTitle(string typeTitle) =>
			WriteLine(typeTitle);

		protected override void WriteUsagesTitle(string usagesTitle) =>
			WriteLine(usagesTitle);

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