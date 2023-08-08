using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using CoreCompatibilyzer.ApiData.Model;
using CoreCompatibilyzer.Constants;
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

		public HashSet<string> UsedNamespaces { get; } = new();

		public HashSet<string> UsedBannedTypes { get; } = new();

		public DiagnosticsWithBannedApis()
        {
			_diagnosticsWithApis	= new();
			UnrecognizedDiagnostics = new List<Diagnostic>();
		}

        public DiagnosticsWithBannedApis(IEnumerable<Diagnostic> diagnostics)
        {
			var diagnosticsWithApisLookup = diagnostics.ThrowIfNull(nameof(diagnostics))
													   .Select(diagnostic => (Diagnostic: diagnostic, BannedApi: GetBannedApiFromDiagnostic(diagnostic)))
													   .ToLookup(d => d.BannedApi != null);

			UnrecognizedDiagnostics = diagnosticsWithApisLookup[false].Select(d => d.Diagnostic).ToList();

			IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> diagnosticsWithSuccessfullyReadApis = diagnosticsWithApisLookup[true]!;
			int? estimatedCapacity = (diagnostics as IReadOnlyCollection<Diagnostic>)?.Count;
			_diagnosticsWithApis = estimatedCapacity.HasValue
				? diagnosticsWithSuccessfullyReadApis.ToList(estimatedCapacity.Value)
				: diagnosticsWithSuccessfullyReadApis.ToList();

			foreach (var (_, bannedApi) in _diagnosticsWithApis)
			{
				if (bannedApi.Kind == ApiKind.Type)
					UsedBannedTypes.Add(bannedApi.FullTypeName);
				else if (bannedApi.Kind == ApiKind.Namespace)
					UsedNamespaces.Add(bannedApi.Namespace);
			}
		}

		private Api? GetBannedApiFromDiagnostic(Diagnostic diagnostic)
		{
			if (diagnostic.Properties.Count == 0 ||
				!diagnostic.Properties.TryGetValue(CommonConstants.ApiDataProperty, out string? rawApiData) || rawApiData.IsNullOrWhiteSpace())
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

		public List<(Diagnostic Diagnostic, Api BannedApi)>.Enumerator GetEnumerator() => 
			_diagnosticsWithApis.GetEnumerator();

		IEnumerator<(Diagnostic Diagnostic, Api BannedApi)> IEnumerable<(Diagnostic Diagnostic, Api BannedApi)>.GetEnumerator() =>
			GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
