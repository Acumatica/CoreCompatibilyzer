using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using CoreCompatibilyzer.ApiData.Providers;
using CoreCompatibilyzer.ApiData.Storage;
using CoreCompatibilyzer.StaticAnalysis.ApiInfoRetrievers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;


namespace CoreCompatibilyzer.StaticAnalysis
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public partial class CoreCompatibilyzerAnalyzer : DiagnosticAnalyzer
	{
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		   ImmutableArray.Create(
			   Descriptors.CoreCompat1001_ApiNotPresentInDotNetCore,
			   Descriptors.CoreCompat1002_ApiObsoleteInDotNetCore);

		private readonly IApiStorage? _customBannedApi;
		private readonly IApiDataProvider? _customBannedApiDataProvider;

		private readonly IApiStorage? _customWhiteList;
		private readonly IApiDataProvider? _customWhiteListDataProvider;

		private readonly IApiInfoRetriever? _customBanInfoRetriever;
		private readonly IApiInfoRetriever? _customWhiteListInfoRetriever;

        public CoreCompatibilyzerAnalyzer() : this(customBannedApi: null, customBannedApiDataProvider: null, 
												   customWhiteList: null, customWhiteListDataProvider: null,
												   customBanInfoRetriever: null, customWhiteListInfoRetriever: null)
        {          
        }

		public CoreCompatibilyzerAnalyzer(IApiStorage? customBannedApi, IApiDataProvider? customBannedApiDataProvider, 
										  IApiStorage? customWhiteList, IApiDataProvider? customWhiteListDataProvider,
										  IApiInfoRetriever? customBanInfoRetriever, IApiInfoRetriever? customWhiteListInfoRetriever)
		{
			_customBannedApi 			  = customBannedApi;
			_customBannedApiDataProvider  = customBannedApiDataProvider;
			_customWhiteList 			  = customWhiteList;
			_customWhiteListDataProvider  = customWhiteListDataProvider;
			_customBanInfoRetriever 	  = customBanInfoRetriever;
			_customWhiteListInfoRetriever = customWhiteListInfoRetriever;
		}

		public sealed override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None); // We want to analyze for compatibility even generated code

			if (!Debugger.IsAttached)
			{
				context.EnableConcurrentExecution();
			}
				
			context.RegisterCompilationStartAction(AnalyzeCompilation);
		}

		private void AnalyzeCompilation(CompilationStartAnalysisContext compilationStartContext)
		{
			compilationStartContext.CancellationToken.ThrowIfCancellationRequested();

			var bannedApiStorage = _customBannedApi ?? GetBannedApiStorage(compilationStartContext.CancellationToken);

			if (bannedApiStorage.ApiKindsCount == 0)
				return;

			compilationStartContext.CancellationToken.ThrowIfCancellationRequested();

			var apiBanInfoRetriever = _customBanInfoRetriever ?? GetApiBanInfoRetriever(bannedApiStorage);

			if (apiBanInfoRetriever == null)
				return;

			var whiteListStorage = _customWhiteList ?? GetWhiteListStorage(compilationStartContext.CancellationToken);
			var whiteListInfoRetriever = _customWhiteListInfoRetriever ?? GetWhiteListInfoRetriever(whiteListStorage);

			compilationStartContext.RegisterSyntaxNodeAction(context => AnalyzeSyntaxTree(context, apiBanInfoRetriever, whiteListInfoRetriever), 
															 SyntaxKind.CompilationUnit);
		}

		protected virtual IApiStorage GetBannedApiStorage(CancellationToken cancellation) =>
			ApiStorage.BannedApi.GetStorage(cancellation, _customBannedApiDataProvider);

		protected virtual IApiStorage GetWhiteListStorage(CancellationToken cancellation) =>
			ApiStorage.WhiteList.GetStorage(cancellation, _customWhiteListDataProvider);

		protected virtual IApiInfoRetriever GetApiBanInfoRetriever(IApiStorage bannedApiStorage) =>
			GetHierarchicalApiInfoRetrieverWithCache(bannedApiStorage);

		protected virtual IApiInfoRetriever? GetWhiteListInfoRetriever(IApiStorage whiteListStorage) =>
			whiteListStorage.ApiKindsCount > 0
				? GetHierarchicalApiInfoRetrieverWithCache(whiteListStorage)
				: null;

		protected IApiInfoRetriever GetHierarchicalApiInfoRetrieverWithCache(IApiStorage storage) =>
			new ApiInfoRetrieverWithWeakCache(
				new HierarchicalApiBanInfoRetriever(storage));

		private void AnalyzeSyntaxTree(in SyntaxNodeAnalysisContext syntaxContext, IApiInfoRetriever apiBanInfoRetriever, 
									   IApiInfoRetriever? whiteListInfoRetriever)
		{
			syntaxContext.CancellationToken.ThrowIfCancellationRequested();

			if (syntaxContext.Node is CompilationUnitSyntax compilationUnitSyntax)
			{
				var apiNodesWalker = new ApiNodesWalker(syntaxContext, apiBanInfoRetriever, whiteListInfoRetriever, checkInterfaces: false);
				apiNodesWalker.CheckSyntaxTree(compilationUnitSyntax);
			}	
		}
	}
}