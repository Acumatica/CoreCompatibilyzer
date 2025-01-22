using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using CoreCompatibilyzer.ApiData.Model;
using CoreCompatibilyzer.Constants;
using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Utils.Common;

using Microsoft.CodeAnalysis;

namespace CoreCompatibilyzer.Runner.Output.Data
{
	internal class DiagnosticsWithBannedApis : IReadOnlyList<(Diagnostic Diagnostic, Api BannedApi)>
	{
		private readonly List<(Diagnostic Diagnostic, Api BannedApi)> _diagnosticsWithApis;

		public IReadOnlyList<Diagnostic> UnrecognizedDiagnostics { get; }

		public int Count => _diagnosticsWithApis.Count;

		public int TotalDiagnosticsCount => Count + UnrecognizedDiagnostics.Count;

		public (Diagnostic Diagnostic, Api BannedApi) this[int index] => _diagnosticsWithApis[index];

		public UsedDistinctApisCalculator DistinctApisCalculator { get; }

		public IReadOnlyList<Api> UsedDistinctApis { get; }

		public HashSet<string> UsedNamespaces { get; } = new();

		public HashSet<string> UsedBannedTypes { get; } = new();

        public DiagnosticsWithBannedApis(IEnumerable<Diagnostic> diagnostics, AppAnalysisContext analysisContext)
        {
			analysisContext.ThrowIfNull(nameof(analysisContext));

			var diagnosticsWithApisLookup = diagnostics.ThrowIfNull(nameof(diagnostics))
													   .Select(diagnostic => (Diagnostic: diagnostic, BannedApi: GetBannedApiFromDiagnostic(diagnostic)))
													   .ToLookup(d => d.BannedApi != null);

			UnrecognizedDiagnostics = diagnosticsWithApisLookup[false].Select(d => d.Diagnostic).ToList();

			IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> diagnosticsWithSuccessfullyReadApis = diagnosticsWithApisLookup[true]!;
			int? estimatedCapacity = (diagnostics as IReadOnlyCollection<Diagnostic>)?.Count;
			_diagnosticsWithApis = estimatedCapacity.HasValue
				? diagnosticsWithSuccessfullyReadApis.ToList(estimatedCapacity.Value)
				: diagnosticsWithSuccessfullyReadApis.ToList();

			FillBannedNamespacesAndTypes();

			DistinctApisCalculator = new UsedDistinctApisCalculator(analysisContext, UsedNamespaces, UsedBannedTypes);
			UsedDistinctApis	   = DistinctApisCalculator.GetAllUsedApis(_diagnosticsWithApis).ToList();
		}

		internal DiagnosticsWithBannedApis(IEnumerable<(Diagnostic Diagnostic, Api? BannedApi)> diagnosticsWithApis, AppAnalysisContext analysisContext)
		{
			analysisContext.ThrowIfNull(nameof(analysisContext));
			diagnosticsWithApis.ThrowIfNull(nameof(diagnosticsWithApis));

			int? estimatedCapacity = (diagnosticsWithApis as IReadOnlyCollection<(Diagnostic Diagnostic, Api BannedApi)>)?.Count;
			IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> diagnosticsWithSuccessfullyReadApisQuery = diagnosticsWithApis.Where(d => d.BannedApi != null)!;
			
			_diagnosticsWithApis = estimatedCapacity.HasValue
				? diagnosticsWithSuccessfullyReadApisQuery.ToList(estimatedCapacity.Value)
				: diagnosticsWithSuccessfullyReadApisQuery.ToList();

			if (_diagnosticsWithApis.Count == estimatedCapacity)
				UnrecognizedDiagnostics = Array.Empty<Diagnostic>();
			else
			{
				var unrecognizedDiagnosticsQuery = diagnosticsWithApis.Where(d => d.BannedApi == null)
																	  .Select(d => d.Diagnostic);
				UnrecognizedDiagnostics = estimatedCapacity.HasValue
					? unrecognizedDiagnosticsQuery.ToList(estimatedCapacity.Value - _diagnosticsWithApis.Count)
					: unrecognizedDiagnosticsQuery.ToList();
			}

			FillBannedNamespacesAndTypes();

			DistinctApisCalculator = new UsedDistinctApisCalculator(analysisContext, UsedNamespaces, UsedBannedTypes);
			UsedDistinctApis 	   = DistinctApisCalculator.GetAllUsedApis(_diagnosticsWithApis).ToList();
		}

		private Api? GetBannedApiFromDiagnostic(Diagnostic diagnostic)
		{
			if (diagnostic.Properties.Count == 0 ||
				!diagnostic.Properties.TryGetValue(CommonConstants.ClosestBannedApiProperty, out string? rawApiData) || rawApiData.IsNullOrWhiteSpace())
			{
				return null;
			}

			try
			{
				return new Api(rawApiData);
			}
			catch (Exception e)
			{
				Serilog.Log.Error(e, "Error during the diagnostic output analysis");
				return null;
			}
		}

		private void FillBannedNamespacesAndTypes()
		{
			foreach (var (_, bannedApi) in _diagnosticsWithApis)
			{
				if (bannedApi.Kind == ApiKind.Type)
					UsedBannedTypes.Add(bannedApi.FullTypeName);
				else if (bannedApi.Kind == ApiKind.Namespace)
					UsedNamespaces.Add(bannedApi.Namespace);
			}
		}

		public List<(Diagnostic Diagnostic, Api BannedApi)>.Enumerator GetEnumerator() => 
			_diagnosticsWithApis.GetEnumerator();

		IEnumerator<(Diagnostic Diagnostic, Api BannedApi)> IEnumerable<(Diagnostic Diagnostic, Api BannedApi)>.GetEnumerator() =>
			GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
