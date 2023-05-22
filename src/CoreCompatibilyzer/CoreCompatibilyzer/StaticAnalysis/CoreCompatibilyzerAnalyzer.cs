using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using CoreCompatibilyzer.BannedApiData.Model;
using CoreCompatibilyzer.BannedApiData.Providers;
using CoreCompatibilyzer.BannedApiData.Storage;
using CoreCompatibilyzer.StaticAnalysis.BannedApiRetriever;
using CoreCompatibilyzer.Utils.Common;
using CoreCompatibilyzer.Utils.Roslyn.Suppression;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;


namespace CoreCompatibilyzer.StaticAnalysis
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class CoreCompatibilyzerAnalyzer : DiagnosticAnalyzer
	{
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		   ImmutableArray.Create(
			   Descriptors.CoreCompat1001_ApiNotPresentInDotNetCore,
			   Descriptors.CoreCompat1002_ApiObsoleteInDotNetCore);

		protected virtual ImmutableArray<SymbolKind> SymbolKindsToAnalyze =>
			new[]
			{
				SymbolKind.Namespace,
				SymbolKind.NamedType,
				SymbolKind.Method,
				SymbolKind.Property,
				SymbolKind.Event,
				SymbolKind.Field,
				SymbolKind.Parameter,
				SymbolKind.TypeParameter,
				SymbolKind.Local,
				SymbolKind.ArrayType,
				SymbolKind.PointerType
			}
			.ToImmutableArray();

		private readonly IBannedApiStorage? _customBannedApi;
		private readonly IApiBanInfoRetriever? _customBanInfoRetriever;

        public CoreCompatibilyzerAnalyzer() : this(customBannedApi: null, customBanInfoRetriever: null)
        {          
        }

		public CoreCompatibilyzerAnalyzer(IBannedApiStorage? customBannedApi, IApiBanInfoRetriever? customBanInfoRetriever)
		{
			_customBannedApi = customBannedApi;
			_customBanInfoRetriever = customBanInfoRetriever;
		}

		public sealed override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze); // We want to analyze for compatibility even generated code
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction(AnalyzeCompilation);
		}

		private void AnalyzeCompilation(CompilationStartAnalysisContext compilationStartContext)
		{
			compilationStartContext.CancellationToken.ThrowIfCancellationRequested();

			var symbolKindsToAnalyze = SymbolKindsToAnalyze;

			if (symbolKindsToAnalyze.IsDefaultOrEmpty)
				return;

			var bannedApiStorage = _customBannedApi ?? GetBannedApiStorage(compilationStartContext.CancellationToken);

			if (bannedApiStorage?.BannedApiKindsCount is null or 0)
				return;

			compilationStartContext.CancellationToken.ThrowIfCancellationRequested();

			var apiBanInfoRetriever = _customBanInfoRetriever ?? GetApiBanInfoRetriever(bannedApiStorage);

			if (apiBanInfoRetriever == null)
				return;

			compilationStartContext.RegisterSymbolAction(context => AnalyzeSymbol(context, bannedApiStorage, apiBanInfoRetriever), symbolKindsToAnalyze);
		}

		protected virtual IBannedApiStorage GetBannedApiStorage(CancellationToken cancellation, IBannedApiDataProvider? customBannedApiDataProvider = null) =>
			BannedApiStorage.GetStorage(cancellation, customBannedApiDataProvider);

		protected virtual IApiBanInfoRetriever GetApiBanInfoRetriever(IBannedApiStorage bannedApiStorage) =>
			new HierarchicalApiBanInfoRetriever(bannedApiStorage);

		private void AnalyzeSymbol(in SymbolAnalysisContext symbolAnalysisContext, IBannedApiStorage bannedApiStorage, IApiBanInfoRetriever apiBanInfoRetriever)
		{
			symbolAnalysisContext.CancellationToken.ThrowIfCancellationRequested();

			if (apiBanInfoRetriever.GetBanInfoForApi(symbolAnalysisContext.Symbol) is not BannedApi banApiInfo) 
				return;

			var diagnosticDescriptor = GetDiagnosticFromBannedApiInfo(banApiInfo);

			if (diagnosticDescriptor == null)
				return;

			symbolAnalysisContext.CancellationToken.ThrowIfCancellationRequested();

			var locations = symbolAnalysisContext.Symbol.Locations;

			foreach (var location in locations) 
			{
				symbolAnalysisContext.ReportDiagnosticWithSuppressionCheck(
					Diagnostic.Create(diagnosticDescriptor, location, banApiInfo.FullName));
			}
		}

		private DiagnosticDescriptor? GetDiagnosticFromBannedApiInfo(in BannedApi banApiInfo) => banApiInfo.BannedApiType switch
		{
			BannedApiType.NotPresentInNetCore => Descriptors.CoreCompat1001_ApiNotPresentInDotNetCore,
			BannedApiType.Obsolete			  => Descriptors.CoreCompat1002_ApiObsoleteInDotNetCore,
			_								  => null
		};
	}
}
