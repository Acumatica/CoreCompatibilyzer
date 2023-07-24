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
		private readonly List<Diagnostic> _unrecognizedDiagnostics = new();

		public void OutputDiagnostics(ImmutableArray<Diagnostic> diagnostics, AppAnalysisContext analysisContext, CancellationToken cancellation)
		{
			_unrecognizedDiagnostics.Clear();

			if (diagnostics.IsDefaultOrEmpty)
				return;

#pragma warning disable Serilog004 // Constant MessageTemplate verifier
			Log.Error("Analysis found {ErrorCount}" + Environment.NewLine, diagnostics.Length);
#pragma warning restore Serilog004 // Constant MessageTemplate verifier

			cancellation.ThrowIfCancellationRequested();

			var diagnosticsWithApis = (from diagnostic in diagnostics
									  let api = GetBannedApiFromDiagnostic(diagnostic)
									  where api != null!
									  select (Diagnostic: diagnostic, BannedApi: api)
									  )
									  .ToList();

			HashSet<string> usedNamespaces = new();
			HashSet<string> usedBannedTypes = new();

			foreach (var d in diagnosticsWithApis)
			{
				if (d.BannedApi.Kind == ApiKind.Type)
					usedBannedTypes.Add(d.BannedApi.FullTypeName);
				else if (d.BannedApi.Kind == ApiKind.Namespace)
					usedNamespaces.Add(d.BannedApi.Namespace);
			}

			if (analysisContext.Grouping.HasGrouping(GroupingMode.Namespaces))
			{
				var groupedByNamespaces = diagnosticsWithApis.GroupBy(d => d.BannedApi.Namespace)
															 .OrderBy(diagnosticsByNamespaces => diagnosticsByNamespaces.Key);

				foreach (var namespaceDiagnostics in groupedByNamespaces)
				{
					cancellation.ThrowIfCancellationRequested();
					OutputNamespaceDiagnosticGroup(namespaceDiagnostics.Key, analysisContext, depth: 0, namespaceDiagnostics,
												   usedBannedTypes, usedNamespaces, cancellation);
				}

				ReportUnrecognizedDiagnostics();
				return;
			}

			if (analysisContext.Grouping.HasGrouping(GroupingMode.Types))
			{
				var namespacesAndOtherApis = diagnosticsWithApis.ToLookup(d => d.BannedApi.Kind == ApiKind.Namespace);
				var namespacesApis = namespacesAndOtherApis[true];
				var otherApis = namespacesAndOtherApis[false];

				OutputNamespaceDiagnosticsSectionForTypesOnlyGrouping(analysisContext, namespacesApis, usedBannedTypes);
				cancellation.ThrowIfCancellationRequested();

				var groupedByTypes = otherApis.GroupBy(d => d.BannedApi.FullTypeName)
											  .OrderBy(diagnosticsByTypes => diagnosticsByTypes.Key);

				foreach (var typeDiagnostics in groupedByTypes)
				{
					cancellation.ThrowIfCancellationRequested();
					OutputTypeDiagnosticGroup(typeDiagnostics.Key, analysisContext, depth: 0, typeDiagnostics, usedBannedTypes);
				}

				ReportUnrecognizedDiagnostics();
				return;
			}

			var sortedFlatDiagnostics = diagnosticsWithApis.OrderBy(d => d.BannedApi.FullName);
			OutputDiagnosticGroup(analysisContext, depth: 0, sortedFlatDiagnostics, usedBannedTypes);
			ReportUnrecognizedDiagnostics();
		}

		private Api? GetBannedApiFromDiagnostic(Diagnostic diagnostic)
		{
			if (diagnostic.Properties.Count == 0 ||
				!diagnostic.Properties.TryGetValue(CommonConstants.ApiDataProperty, out string? rawApiData) || rawApiData.IsNullOrWhiteSpace())
			{
				_unrecognizedDiagnostics.Add(diagnostic);
				return null;
			}

			try
			{
				return new Api(rawApiData);
			}
			catch (Exception e)
			{
				Log.Error(e, "Error during the diagnostic output analysis");
				_unrecognizedDiagnostics.Add(diagnostic);

				return null;
			}
		}

		private void OutputNamespaceDiagnosticGroup(string @namespace, AppAnalysisContext analysisContext, int depth,
													IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> diagnostics, HashSet<string> usedBannedTypes,
													HashSet<string> usedNamespaces, CancellationToken cancellation)
		{
			string namespacePadding = GetPadding(depth);
			OutputTitle(namespacePadding + @namespace, ConsoleColor.DarkCyan);
			Console.WriteLine();
			cancellation.ThrowIfCancellationRequested();

			if (usedNamespaces.Contains(@namespace) && analysisContext.Format == FormatMode.UsedAPIsWithUsages)
			{
				diagnostics = diagnostics.ToList();
				var namespaceUsages = diagnostics.Where(d => d.BannedApi.Kind == ApiKind.Namespace)
												 .Select(d => d.Diagnostic);
				OutputApiUsages(@namespace, depth + 1, namespaceUsages);
				Console.WriteLine();
			}

			string subSectionPadding = GetPadding(depth + 1);
			OutputTitle(subSectionPadding + "Members:", ConsoleColor.Gray);
			cancellation.ThrowIfCancellationRequested();

			var namespaceMembers = diagnostics.Where(d => d.BannedApi.Kind != ApiKind.Namespace);

			if (analysisContext.Grouping.HasGrouping(GroupingMode.Types))
			{
				var groupedByTypes = namespaceMembers.GroupBy(d => d.BannedApi.FullTypeName)
													 .OrderBy(diagnosticsByTypes => diagnosticsByTypes.Key);

				foreach (var typeDiagnostics in groupedByTypes)
				{
					cancellation.ThrowIfCancellationRequested();

					string typeName = typeDiagnostics.Key;
					OutputTypeDiagnosticGroup(typeName, analysisContext, depth + 2, typeDiagnostics, usedBannedTypes);
				}

				return;
			}
			else
			{
				OutputDiagnosticGroup(analysisContext, depth + 2, namespaceMembers, usedBannedTypes);
			}
		}

		private void OutputTypeDiagnosticGroup(string typeName, AppAnalysisContext analysisContext, int depth,
											   IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> diagnostics,
											   HashSet<string> usedBannedTypes)
		{
			string typeNamePadding = GetPadding(depth);
			OutputTitle(typeNamePadding + typeName, ConsoleColor.Magenta);
			Console.WriteLine();

			if (usedBannedTypes.Contains(typeName))
			{
				if (analysisContext.Format == FormatMode.UsedAPIsOnly)
					return;

				diagnostics    = diagnostics.ToList();
				var typeUsages = diagnostics.Where(d => d.BannedApi.Kind == ApiKind.Type)
											.Select(d => d.Diagnostic);
				OutputApiUsages(typeName, depth + 1, typeUsages);
				Console.WriteLine();
			}

			string subSectionPadding = GetPadding(depth + 1);
			OutputTitle(subSectionPadding + "Members:", ConsoleColor.Gray);

			var memberUsages = diagnostics.Where(d => d.BannedApi.Kind != ApiKind.Type);
			OutputDiagnosticGroup(analysisContext, depth + 2, memberUsages, usedBannedTypes);
			Console.WriteLine();
		}

		private void OutputNamespaceDiagnosticsSectionForTypesOnlyGrouping(AppAnalysisContext analysisContext, 
																		   IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> diagnostics,
																		   HashSet<string> usedBannedTypes)
		{
			if (!diagnostics.Any())
				return;

			string sectionPadding = GetPadding(depth: 0);
			OutputTitle(sectionPadding + "Namespaces:", ConsoleColor.DarkCyan);
			Console.WriteLine();

			OutputDiagnosticGroup(analysisContext, depth: 1, diagnostics, usedBannedTypes);
		}

		private void OutputDiagnosticGroup(AppAnalysisContext analysisContext, int depth, IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> diagnostics,
										   HashSet<string> usedBannedTypes)
		{
			string padding = GetPadding(depth);

			if (analysisContext.Format == FormatMode.UsedAPIsOnly)
			{
				var allApis = GetUsedApisOnly(analysisContext, diagnostics, usedBannedTypes);

				foreach (string api in allApis)
					OutputFoundBannedApi(api, padding, addListItems: true);
			}
			else
			{
				string apiNamePadding		= GetPadding(depth);
				var diagnosticsGroupedByApi = diagnostics.GroupBy(d => d.BannedApi.FullName)
														 .OrderBy(d => d.Key);
				foreach (var diagnosticsByApi in diagnosticsGroupedByApi)
				{
					string apiName = diagnosticsByApi.Key;
					var apiDiagnostics = diagnosticsByApi.Select(d => d.Diagnostic)
														 .OrderBy(d => d.Location.SourceTree?.FilePath ?? string.Empty);

					OutputFoundBannedApi(apiName, apiNamePadding, addListItems: false);
					Console.WriteLine();
					OutputApiUsages(apiName, depth + 1, apiDiagnostics);
				}
			}
		}

		private IEnumerable<string> GetUsedApisOnly(AppAnalysisContext analysisContext, IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> diagnostics,
													HashSet<string> usedBannedTypes)
		{
			var sortedUsedApi = diagnostics.Select(d => d.BannedApi)
										   .Distinct()
										   .OrderBy(api => api.FullName);
			foreach (Api api in sortedUsedApi)
			{
				switch (api.Kind)
				{
					case ApiKind.Namespace:
					case ApiKind.Type 
					when api.ContainingTypes.IsDefaultOrEmpty || !IsContainingTypeUsedBannedType(api):
						yield return api.FullName;
						continue;

					case ApiKind.Field:
					case ApiKind.Property:
					case ApiKind.Event:
					case ApiKind.Method:
						if (!usedBannedTypes.Contains(api.FullTypeName))
							yield return api.FullName;

						continue;
				}
			}

			//------------------------------------Local Function------------------------------------------
			bool IsContainingTypeUsedBannedType(Api api)
			{
				string containingTypeName = $"{api.Namespace}";

				for (int i = 0; i < api.ContainingTypes.Length; i++)
				{
					containingTypeName += $".{api.ContainingTypes[i]}";

					if (usedBannedTypes!.Contains(containingTypeName))
						return true;
				}

				return false;
			}
		}

		private void OutputApiUsages(string apiName, int depth, IEnumerable<Diagnostic> diagnostics)
		{
			string usagesSectionPadding = GetPadding(depth);
			OutputTitle(usagesSectionPadding + "Usages:", ConsoleColor.Blue);

			string usagesPadding = GetPadding(depth + 1);

			foreach (Diagnostic diagnostic in diagnostics) 
			{
				LogErrorForFoundDiagnosticWithUsage(apiName, diagnostic, usagesPadding);
			}
		}

		private void OutputFoundBannedApi(string apiName, string padding, bool addListItems)
		{
			if (addListItems)
				Console.WriteLine($"{padding}-{apiName}");
			else
				Console.WriteLine($"{padding}{apiName}");
		}

		private void LogErrorForFoundDiagnosticWithUsage(string apiName, Diagnostic diagnostic, string padding)
		{
			var prettyLocation = diagnostic.Location.GetMappedLineSpan().ToString();
			var diagnosticMessage = string.Format(diagnostic.Descriptor.Title.ToString(), apiName);
			string errorMsgTemplate = $"{padding}-{{Id}} {{Severity}} {{Location}}:{Environment.NewLine}{{Description}}";

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

			var sortedDiagnostics = _unrecognizedDiagnostics.OrderBy(d => d.Location.SourceTree?.FilePath ?? string.Empty);

			foreach (Diagnostic diagnostic in sortedDiagnostics)
			{
				LogMessage(diagnostic.Severity, diagnostic.ToString(), messageArgs: null);
			}
		}

		private string GetPadding(int depth)
		{
			const int paddingMultiplier = 4;
			string padding = depth <= 0
				? string.Empty
				: new string(' ', depth * paddingMultiplier);
			
			return string.Intern(padding);
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
