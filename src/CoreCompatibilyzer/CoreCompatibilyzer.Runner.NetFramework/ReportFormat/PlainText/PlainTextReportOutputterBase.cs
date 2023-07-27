using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using CoreCompatibilyzer.ApiData.Model;
using CoreCompatibilyzer.Constants;
using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Runner.Output;
using CoreCompatibilyzer.Utils.Common;

using Microsoft.CodeAnalysis;

namespace CoreCompatibilyzer.Runner.NetFramework.ReportFormat.PlainText
{
	/// <summary>
	/// The base class for the report outputter in the plain text format.
	/// </summary>
	internal abstract class PlainTextReportOutputterBase : IReportOutputter
	{
		protected abstract void WriteLine();

		protected void WriteLine<T>(T obj)
		{
			if (obj is null)
				WriteLine();
			else
				WriteLine(obj.ToString());
		}

		protected abstract void WriteLine(string text);

		protected abstract void WriteAllApisTitle(string allApisTitle);

		protected abstract void WriteNamespaceTitle(string namespaceTitle);

		protected abstract void WriteTypeTitle(string typeTitle);

		protected abstract void WriteTypeMembersTitle(string typeMembersTitle);

		protected abstract void WriteApiTitle(string apiTitle);

		protected abstract void WriteUsagesTitle(string usagesTitle);

		protected abstract void WriteFlatApiUsage(string fullApiName, string location);

		public virtual void OutputDiagnostics(ImmutableArray<Diagnostic> diagnostics, AppAnalysisContext analysisContext, CancellationToken cancellation)
		{
			if (diagnostics.IsDefaultOrEmpty)
				return;

			WriteLine($"Total errors count: {diagnostics.Length}");
			cancellation.ThrowIfCancellationRequested();

			List<Diagnostic> unrecognizedDiagnostics = new();
			var diagnosticsWithApis = (from diagnostic in diagnostics
									   let api = GetBannedApiFromDiagnostic(diagnostic, unrecognizedDiagnostics)
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
				OutputReportGroupedByNamespaces(analysisContext, diagnosticsWithApis, usedNamespaces, usedBannedTypes, cancellation);
			}
			else if (analysisContext.Grouping.HasGrouping(GroupingMode.Types))
			{
				OutputReportGroupedOnlyByTypes(analysisContext, diagnosticsWithApis, usedBannedTypes, cancellation);
			}
			else
			{
				WriteAllApisTitle("Found APIs:");
				var sortedFlatDiagnostics = diagnosticsWithApis.OrderBy(d => d.BannedApi.FullName);

				OutputDiagnosticGroup(analysisContext, depth: 1, sortedFlatDiagnostics, usedBannedTypes);
			}

			WriteLine();
			ReportUnrecognizedDiagnostics(unrecognizedDiagnostics);
		}

		private void OutputReportGroupedByNamespaces(AppAnalysisContext analysisContext, List<(Diagnostic Diagnostic, Api BannedApi)> diagnosticsWithApis,
													 HashSet<string> usedNamespaces, HashSet<string> usedBannedTypes, CancellationToken cancellation)
		{
			var groupedByNamespaces = diagnosticsWithApis.GroupBy(d => d.BannedApi.Namespace)
														 .OrderBy(diagnosticsByNamespaces => diagnosticsByNamespaces.Key);

			foreach (var namespaceDiagnostics in groupedByNamespaces)
			{
				cancellation.ThrowIfCancellationRequested();
				OutputNamespaceDiagnosticGroup(namespaceDiagnostics.Key, analysisContext, depth: 0, namespaceDiagnostics.ToList(),
											   usedBannedTypes, usedNamespaces, cancellation);
			}
		}

		private void OutputReportGroupedOnlyByTypes(AppAnalysisContext analysisContext, List<(Diagnostic Diagnostic, Api BannedApi)> diagnosticsWithApis,
													HashSet<string> usedBannedTypes, CancellationToken cancellation)
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
				OutputTypeDiagnosticGroup(typeDiagnostics.Key, analysisContext, depth: 0, typeDiagnostics.ToList(), usedBannedTypes);
			}
		}

		private void OutputNamespaceDiagnosticGroup(string @namespace, AppAnalysisContext analysisContext, int depth,
													List<(Diagnostic Diagnostic, Api BannedApi)> diagnostics, HashSet<string> usedBannedTypes,
													HashSet<string> usedNamespaces, CancellationToken cancellation)
		{
			string namespacePadding = GetPadding(depth);
			WriteNamespaceTitle(namespacePadding + @namespace);

			cancellation.ThrowIfCancellationRequested();

			bool groupByApis  = analysisContext.Grouping.HasGrouping(GroupingMode.Apis);
			bool groupByTypes = analysisContext.Grouping.HasGrouping(GroupingMode.Types);

			if (!groupByApis && !groupByTypes)
			{
				OutputFlatApiUsages(depth + 1, diagnostics);
				WriteLine();
				return;
			}

			if (usedNamespaces.Contains(@namespace) && analysisContext.Format == FormatMode.UsedAPIsWithUsages)
			{
				var namespaceDiagnostics = diagnostics.Where(d => d.BannedApi.Kind == ApiKind.Namespace);

				if (groupByApis)
				{
					var sortedNamespaceUsages = namespaceDiagnostics.OrderBy(d => d.Diagnostic.Location.SourceTree?.FilePath ?? string.Empty)
																	.Select(d => d.Diagnostic);
					OutputApiUsages(depth + 1, sortedNamespaceUsages);
				}
				else
				{
					OutputFlatApiUsages(depth + 1, namespaceDiagnostics);
				}
			}

			cancellation.ThrowIfCancellationRequested();
			var namespaceMembers = diagnostics.Where(d => d.BannedApi.Kind != ApiKind.Namespace).ToList();

			if (namespaceMembers.Count == 0)
			{
				WriteLine();
				return;
			}

			string subSectionPadding = GetPadding(depth + 1);
			WriteTypeMembersTitle(subSectionPadding + "Members:");

			if (groupByTypes)
			{
				var groupedByTypes = namespaceMembers.GroupBy(d => d.BannedApi.FullTypeName)
													 .OrderBy(diagnosticsByTypes => diagnosticsByTypes.Key);

				foreach (var typeDiagnostics in groupedByTypes)
				{
					cancellation.ThrowIfCancellationRequested();

					string typeName = typeDiagnostics.Key;
					OutputTypeDiagnosticGroup(typeName, analysisContext, depth + 2, typeDiagnostics.ToList(), usedBannedTypes);
				}

				if (analysisContext.Format == FormatMode.UsedAPIsOnly)
					WriteLine();

				return;
			}
			else
			{
				OutputDiagnosticGroup(analysisContext, depth + 2, namespaceMembers, usedBannedTypes);
			}
		}

		private void OutputTypeDiagnosticGroup(string typeName, AppAnalysisContext analysisContext, int depth,
											   List<(Diagnostic Diagnostic, Api BannedApi)> diagnostics,
											   HashSet<string> usedBannedTypes)
		{
			string typeNamePadding = GetPadding(depth);
			WriteTypeTitle(typeNamePadding + typeName);

			if (!analysisContext.Grouping.HasGrouping(GroupingMode.Apis))
			{
				OutputFlatApiUsages(depth + 1, diagnostics);
				WriteLine();
				return;
			}

			if (usedBannedTypes.Contains(typeName))
			{
				if (analysisContext.Format == FormatMode.UsedAPIsOnly)
					return;

				var sortedTypeUsages = from d in diagnostics
									   where d.BannedApi.Kind == ApiKind.Type
									   orderby d.Diagnostic.Location.SourceTree?.FilePath ?? string.Empty
									   select d.Diagnostic;

				OutputApiUsages(depth + 1, sortedTypeUsages);
			}

			var typeMembers = diagnostics.Where(d => d.BannedApi.Kind != ApiKind.Type).ToList();

			if (typeMembers.Count > 0)
			{
				string subSectionPadding = GetPadding(depth + 1);

				WriteTypeMembersTitle(subSectionPadding + "Members:");
				OutputDiagnosticGroup(analysisContext, depth + 2, typeMembers, usedBannedTypes);
			}
			else
			{
				WriteLine();
			}
		}

		private void OutputNamespaceDiagnosticsSectionForTypesOnlyGrouping(AppAnalysisContext analysisContext,
																		   IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> diagnostics,
																		   HashSet<string> usedBannedTypes)
		{
			if (!diagnostics.Any())
				return;

			WriteNamespaceTitle("Namespaces:");
			OutputDiagnosticGroup(analysisContext, depth: 1, diagnostics, usedBannedTypes);
		}

		private void OutputDiagnosticGroup(AppAnalysisContext analysisContext, int depth, IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> unsortedDiagnostics,
										   HashSet<string> usedBannedTypes)
		{
			string padding = GetPadding(depth);

			if (analysisContext.Format == FormatMode.UsedAPIsOnly)
			{
				var allApis = GetAllUsedApis(analysisContext, unsortedDiagnostics, usedBannedTypes);

				foreach (string api in allApis)
					OutputFoundBannedApi(api, padding, useTitle: false);

				WriteLine();
			}
			else if (analysisContext.Grouping.HasGrouping(GroupingMode.Apis))
				OutputApiUsagesGroupedByApi(depth, unsortedDiagnostics);
			else
			{
				OutputFlatApiUsages(depth, unsortedDiagnostics);
				WriteLine();
			}
		}

		private void OutputApiUsagesGroupedByApi(int depth, IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> unsortedDiagnostics)
		{
			string apiNamePadding = GetPadding(depth);
			var diagnosticsGroupedByApi = unsortedDiagnostics.GroupBy(d => d.BannedApi.FullName)
															 .OrderBy(d => d.Key);
			foreach (var diagnosticsByApi in diagnosticsGroupedByApi)
			{
				string apiName = diagnosticsByApi.Key;
				var apiDiagnostics = diagnosticsByApi.Select(d => d.Diagnostic)
													 .OrderBy(d => d.Location.SourceTree?.FilePath ?? string.Empty);

				OutputFoundBannedApi(apiName, apiNamePadding, useTitle: true);
				OutputApiUsages(depth + 1, apiDiagnostics);
				WriteLine();
			}
		}

		private void OutputFlatApiUsages(int depth, IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> unsortedDiagnostics)
		{
			string apiUsagePadding = GetPadding(depth);
			var sortedDiagnostics = unsortedDiagnostics.Select(d => (FullApiName: d.BannedApi.FullName, Location: GetPrettyLocation(d.Diagnostic)))
													   .OrderBy(apiWithLocation => apiWithLocation.FullApiName)
													   .ThenBy(apiWithLocation => apiWithLocation.Location);

			foreach (var (fullApiName, location) in sortedDiagnostics)
				WriteFlatApiUsage(apiUsagePadding + fullApiName, location);
		}

		private IEnumerable<string> GetAllUsedApis(AppAnalysisContext analysisContext, IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> unsortedDiagnostics,
												   HashSet<string> usedBannedTypes)
		{
			var sortedUsedApi = unsortedDiagnostics.Select(d => d.BannedApi)
												   .Distinct()
												   .OrderBy(api => api.FullName);
			foreach (Api api in sortedUsedApi)
			{
				switch (api.Kind)
				{
					case ApiKind.Namespace:
					case ApiKind.Type
					when analysisContext.ShowMembersOfUsedType || api.ContainingTypes.IsDefaultOrEmpty || !IsContainingTypeUsedBannedType(api):
						yield return api.FullName;
						continue;

					case ApiKind.Field:
					case ApiKind.Property:
					case ApiKind.Event:
					case ApiKind.Method:
						if (analysisContext.ShowMembersOfUsedType || !usedBannedTypes.Contains(api.FullTypeName))
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

		private void OutputApiUsages(int depth, IEnumerable<Diagnostic> sortedDiagnostics)
		{
			string usagesSectionPadding = GetPadding(depth);
			WriteUsagesTitle(usagesSectionPadding + "Usages:");

			string usagesPadding = GetPadding(depth + 1);

			foreach (Diagnostic diagnostic in sortedDiagnostics)
			{
				OutputApiUsage(diagnostic, usagesPadding);
			}
		}

		private void OutputFoundBannedApi(string apiName, string padding, bool useTitle)
		{
			if (useTitle)
				WriteApiTitle($"{padding}{apiName}");
			else
				WriteLine($"{padding}{apiName}");
		}

		private void OutputApiUsage(Diagnostic diagnostic, string padding)
		{
			var prettyLocation = GetPrettyLocation(diagnostic);
			WriteLine($"{padding}{prettyLocation}");
		}

		private string GetPrettyLocation(Diagnostic diagnostic) => diagnostic.Location.GetMappedLineSpan().ToString();

		private void ReportUnrecognizedDiagnostics(List<Diagnostic> unrecognizedDiagnostics)
		{
			if (unrecognizedDiagnostics.Count == 0)
				return;

			WriteLine("-----------------------------------------------------------------------------");
			WriteLine("Analysis found unrecognized diagnostics:");

			var sortedDiagnostics = unrecognizedDiagnostics.OrderBy(d => d.Location.SourceTree?.FilePath ?? string.Empty);

			foreach (Diagnostic diagnostic in sortedDiagnostics)
			{
				WriteLine(diagnostic);
			}

			WriteLine();
		}

		private Api? GetBannedApiFromDiagnostic(Diagnostic diagnostic, List<Diagnostic> unrecognizedDiagnostics)
		{
			if (diagnostic.Properties.Count == 0 ||
				!diagnostic.Properties.TryGetValue(CommonConstants.ApiDataProperty, out string? rawApiData) || rawApiData.IsNullOrWhiteSpace())
			{
				unrecognizedDiagnostics.Add(diagnostic);
				return null;
			}

			try
			{
				return new Api(rawApiData);
			}
			catch (Exception e)
			{
				Serilog.Log.Error(e, "Error during the diagnostic output analysis");
				unrecognizedDiagnostics.Add(diagnostic);

				return null;
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
	}
}