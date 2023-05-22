using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using CoreCompatibilyzer.BannedApiData.Providers;
using CoreCompatibilyzer.BannedApiData.Storage;
using CoreCompatibilyzer.StaticAnalysis.BannedApiRetriever;

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

			if (bannedApiStorage?.BannedApiKindsCount is null or 0)
				return;

			compilationStartContext.CancellationToken.ThrowIfCancellationRequested();

			var apiBanInfoRetriever = _customBanInfoRetriever ?? GetApiBanInfoRetriever(bannedApiStorage);

			if (apiBanInfoRetriever == null)
				return;

			compilationStartContext.RegisterSyntaxNodeAction(context => AnalyzeSyntaxTree(context, bannedApiStorage, apiBanInfoRetriever), 
															 SyntaxKind.CompilationUnit);
		}

		protected virtual IBannedApiStorage GetBannedApiStorage(CancellationToken cancellation, IBannedApiDataProvider? customBannedApiDataProvider = null) =>
			BannedApiStorage.GetStorage(cancellation, customBannedApiDataProvider);

		protected virtual IApiBanInfoRetriever GetApiBanInfoRetriever(IBannedApiStorage bannedApiStorage) =>
			new HierarchicalApiBanInfoRetriever(bannedApiStorage);

		private void AnalyzeSyntaxTree(in SyntaxNodeAnalysisContext syntaxContext, IBannedApiStorage bannedApiStorage, IApiBanInfoRetriever apiBanInfoRetriever)
		{
			syntaxContext.CancellationToken.ThrowIfCancellationRequested();

			if (syntaxContext.Node is CompilationUnitSyntax compilationUnitSyntax)
			{
				var apiNodesWalker = new ApiNodesWalker(syntaxContext, bannedApiStorage, apiBanInfoRetriever);
				compilationUnitSyntax.Accept(apiNodesWalker);
			}	
		}
	}
}