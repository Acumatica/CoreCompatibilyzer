using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

using CoreCompatibilyzer.ApiData.Model;
using CoreCompatibilyzer.ApiData.Storage;
using CoreCompatibilyzer.Constants;
using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Utils.Common;

using Microsoft.CodeAnalysis;

using Serilog;

namespace CoreCompatibilyzer.Runner.ReportFormat
{
	/// <summary>
	/// The standard output formatter.
	/// </summary>
	internal class ReportOutputter : IReportOutputter
	{
		private readonly IApiStorage _bannedApiStorage;
		private readonly IApiStorage _whiteListStorage;

		private readonly List<Diagnostic> _unrecognizedDiagnostics = new();

		public ReportOutputter(IApiStorage bannedApiStorage, IApiStorage whiteListStorage)
		{
			_bannedApiStorage = bannedApiStorage.ThrowIfNull(nameof(bannedApiStorage));
			_whiteListStorage = whiteListStorage.ThrowIfNull(nameof(whiteListStorage));
		}

		public void OutputDiagnostics(ImmutableArray<Diagnostic> diagnostics, AppAnalysisContext analysisContext, CancellationToken cancellation)
		{
			_unrecognizedDiagnostics.Clear();

			if (diagnostics.IsDefaultOrEmpty)
				return;

#pragma warning disable Serilog004 // Constant MessageTemplate verifier
			Log.Error("Analysis found {ErrorCount}" + Environment.NewLine, diagnostics.Length);
#pragma warning restore Serilog004 // Constant MessageTemplate verifier

			cancellation.ThrowIfCancellationRequested();

			var diagnosticsWithApis = from diagnostic in diagnostics
									  let api = GetBannedApiFromDiagnostic(diagnostic)
									  where api != null
									  select (Diagnostic: diagnostic, BannedApi: api.Value);

			if (analysisContext.Grouping.HasGrouping(GroupingMode.Namespaces))
			{
				var groupedByNamespaces = diagnosticsWithApis.GroupBy(d => d.BannedApi.GetNamespace())
															 .OrderBy(diagnosticsByNamespaces => diagnosticsByNamespaces.Key);

				foreach (var namespaceDiagnostics in groupedByNamespaces)
				{
					OutputNamespaceDiagnosticGroup(namespaceDiagnostics.Key, analysisContext, depth: 0, namespaceDiagnostics);
				}


			}
			else
			{

				var namespacesAndOtherApis = diagnosticsWithApis.ToLookup(d => d.BannedApi.Kind == ApiKind.Namespace);
				var namespacesApis = namespacesAndOtherApis[true];

				OutputNamespaceDiagnosticsSection(analysisContext, namespacesApis);

				var otherApis = namespacesAndOtherApis[false];




				if (analysisContext.Grouping.HasGrouping(GroupingMode.Types))
				{
					var groupedByNamespaces = diagnosticsWithApis.GroupBy(d => d.BannedApi.GetTypeName())
																 .OrderBy(diagnosticsByNamespaces => diagnosticsByNamespaces.Key);
				}

				foreach (Diagnostic diagnostic in diagnostics)
				{
					cancellation.ThrowIfCancellationRequested();
					LogErrorForFoundDiagnostic(diagnostic);
				}
			}

			ReportUnrecognizedDiagnostics();
		}

		private Api? GetBannedApiFromDiagnostic(Diagnostic diagnostic)
		{
			if (diagnostic.Properties.Count == 0 ||
				!diagnostic.Properties.TryGetValue(CommonConstants.ApiDocIDWithObsoletionDiagnosticProperty, out string? docIdWithObsoletion) ||
				docIdWithObsoletion.IsNullOrWhiteSpace())
			{
				_unrecognizedDiagnostics.Add(diagnostic);
				return null;
			}

			try
			{
				return new Api(docIdWithObsoletion);
			}
			catch (Exception e)
			{
				Log.Error(e, "Error during the diagnostic output analysis");
				_unrecognizedDiagnostics.Add(diagnostic);

				return null;
			}
		}

		private void OutputNamespaceDiagnosticGroup(string @namespace, AppAnalysisContext analysisContext, int depth,
													IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> diagnostics)
		{
			string padding = GetPadding(depth);
			OutputTitle(padding + @namespace, ConsoleColor.DarkCyan);

			if (analysisContext.Grouping.HasGrouping(GroupingMode.Types))
			{

			}

		}

		private void OutputTypeDiagnosticGroup(string typeName, AppAnalysisContext analysisContext,
											   IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> diagnostics)
		{

		}

		private void OutputDiagnosticGroup(AppAnalysisContext analysisContext, IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> diagnostics)
		{

		}

		private void OutputNamespaceDiagnosticsSection(AppAnalysisContext analysisContext, IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> diagnostics)
		{
			if (!diagnostics.Any())
				return;

			string sectionPadding = GetPadding(depth: 0);
			OutputTitle(sectionPadding + "Namespaces", ConsoleColor.DarkCyan);
			Console.WriteLine();

			string namespaceItemPadding = GetPadding(depth: 1);

			if (analysisContext.Format == FormatMode.UsedAPIsOnly)
			{
				var namespaces = diagnostics.Select(d => d.BannedApi.GetFullName())
											.Distinct()
											.OrderBy(@namespace => @namespace);

				foreach (var @namespace in namespaces)
					OutputFoundBannedApi(@namespace, namespaceItemPadding);
			}
			else
			{
				var orderedNamespaceDiagnostics = diagnostics.OrderBy(d => d.BannedApi.DocID);

				foreach (var (diagnostic, bannedApi) in orderedNamespaceDiagnostics)
				{
					string @namespace = bannedApi.GetFullName();
					LogErrorForFoundDiagnosticWithUsage(@namespace, diagnostic, namespaceItemPadding);
				}
			}
		}

		private void OutputFoundBannedApi(string apiName, string padding)
		{
			Console.WriteLine(padding + apiName);
		}

		private void LogErrorForFoundDiagnosticWithUsage(string apiName, Diagnostic diagnostic, string padding)
		{
			var prettyLocation = diagnostic.Location.GetMappedLineSpan().ToString();
			var diagnosticMessage = string.Format(diagnostic.Descriptor.Title.ToString(), apiName);
			string errorMsgTemplate = $"{padding}{{Id}} {{Severity}} {{Location}}:{Environment.NewLine}{{Description}}";

			LogMessage(diagnostic.Severity, errorMsgTemplate, diagnostic.Id, diagnostic.Severity, prettyLocation, diagnosticMessage);
		}

		[SuppressMessage("CodeQuality", "Serilog004:Constant MessageTemplate verifier", Justification = "Ok to use runtime dependent new line in message")]
		private void LogMessage(DiagnosticSeverity severity, string message, params object[]? messageArgs)
		{
			switch (severity)
			{
				case DiagnosticSeverity.Error:
					Log.Error(message, messageArgs);
					return;

				case DiagnosticSeverity.Warning:
					Log.Warning(message, messageArgs);
					break;

				case DiagnosticSeverity.Info:
					Log.Information(message, messageArgs);
					break;

				case DiagnosticSeverity.Hidden:
					Log.Debug(message, messageArgs);
					break;
			}
		}

		private void ReportUnrecognizedDiagnostics()
		{
			if (_unrecognizedDiagnostics.Count == 0)
				return;

			#pragma warning disable Serilog004 // Constant MessageTemplate verifier
			Log.Error(Environment.NewLine + Environment.NewLine + "-----------------------------------------------------------------------------" +
					  Environment.NewLine + "Analysis found unrecognized diagnostics" + Environment.NewLine);
			#pragma warning restore Serilog004 // Constant MessageTemplate verifier

			foreach (Diagnostic diagnostic in _unrecognizedDiagnostics)
			{
				LogMessage(diagnostic.Severity, diagnostic.ToString(), messageArgs: null);
			}
		}

		private string GetPadding(int depth)
		{
			const int paddingMultiplier = 4;
			return depth <= 0
				? string.Empty
				: new string(' ', depth * paddingMultiplier);
		}

		private void OutputTitle(string text, ConsoleColor color)
		{
			var oldColor = Console.ForegroundColor;

			try
			{
				Console.ForegroundColor = color;
				Console.WriteLine(text);
				Console.WriteLine();
			}
			finally
			{
				Console.ForegroundColor = oldColor;
			}
		}
	}
}
