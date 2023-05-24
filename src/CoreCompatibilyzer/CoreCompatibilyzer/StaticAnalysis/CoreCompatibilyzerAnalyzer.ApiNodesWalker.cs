using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;
using System.Xml.Linq;

using CoreCompatibilyzer.ApiData.Model;
using CoreCompatibilyzer.ApiData.Storage;
using CoreCompatibilyzer.Constants;
using CoreCompatibilyzer.StaticAnalysis.ApiInfoRetrievers;
using CoreCompatibilyzer.Utils.Common;
using CoreCompatibilyzer.Utils.Roslyn.Semantic;
using CoreCompatibilyzer.Utils.Roslyn.Suppression;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CoreCompatibilyzer.StaticAnalysis
{
	public partial class CoreCompatibilyzerAnalyzer
	{
		private class ApiNodesWalker : CSharpSyntaxWalker
		{
			private readonly SyntaxNodeAnalysisContext _syntaxContext;
			private readonly IApiInfoRetriever _apiBanInfoRetriever;
			private readonly BannedTypesInfoCollector _bannedTypesInfoCollector;

			public bool CheckInterfaces { get; }

			private CancellationToken Cancellation => _syntaxContext.CancellationToken;

			private SemanticModel SemanticModel => _syntaxContext.SemanticModel;

            public ApiNodesWalker(SyntaxNodeAnalysisContext syntaxContext,IApiInfoRetriever apiBanInfoRetriever, bool checkInterfaces)
            {
                _syntaxContext 		 	  = syntaxContext;
				_apiBanInfoRetriever 	  = apiBanInfoRetriever;
				_bannedTypesInfoCollector = new BannedTypesInfoCollector(apiBanInfoRetriever, syntaxContext.CancellationToken);
				CheckInterfaces = checkInterfaces;
			}

			#region Visit XML comments methods to prevent coloring in XML comments don't call base method
			public override void VisitXmlCrefAttribute(XmlCrefAttributeSyntax node) { }

			public override void VisitXmlComment(XmlCommentSyntax node) { }

			public override void VisitCrefBracketedParameterList(CrefBracketedParameterListSyntax node) { }

			public override void VisitDocumentationCommentTrivia(DocumentationCommentTriviaSyntax node) { }

			public override void VisitXmlElement(XmlElementSyntax node) { }

			public override void VisitXmlText(XmlTextSyntax node) { }
			#endregion

			public override void VisitPredefinedType(PredefinedTypeSyntax predefinedType)
			{
				// predefined primitives, such as "string" and "int", should be always compatible
			}

			public override void VisitUsingDirective(UsingDirectiveSyntax usingDirectiveNode)
			{
				Cancellation.ThrowIfCancellationRequested();

				if (usingDirectiveNode.Name == null ||
					SemanticModel.GetSymbolOrFirstCandidate(usingDirectiveNode.Name, Cancellation) is not ISymbol typeOrNamespaceSymbol)
				{
					return;
				}

				switch (typeOrNamespaceSymbol)
				{
					case INamespaceSymbol namespaceSymbol:
						Api? bannedNamespaceOrTypeInfo = _apiBanInfoRetriever.GetInfoForApi(namespaceSymbol);

						if (bannedNamespaceOrTypeInfo.HasValue)
							ReportApi(bannedNamespaceOrTypeInfo.Value, usingDirectiveNode.Name);

						break;
					
					case ITypeSymbol typeSymbol:
						var bannedTypeInfos = _bannedTypesInfoCollector.GetTypeBannedApiInfos(typeSymbol, CheckInterfaces);
						ReportApiList(bannedTypeInfos, usingDirectiveNode.Name);
						break;
				}	
			}

			public override void VisitGenericName(GenericNameSyntax genericNameNode)
			{
				Cancellation.ThrowIfCancellationRequested();

				if (SemanticModel.GetSymbolOrFirstCandidate(genericNameNode, Cancellation) is not ISymbol symbol)
				{
					Cancellation.ThrowIfCancellationRequested();
					base.VisitGenericName(genericNameNode);
					return;
				}

				Cancellation.ThrowIfCancellationRequested();

				if (symbol is ITypeSymbol typeSymbol)
				{
					List<Api>? bannedTypeInfos = typeSymbol is ITypeParameterSymbol typeParameterSymbol
						? _bannedTypesInfoCollector.GetTypeParameterBannedApiInfos(typeParameterSymbol, CheckInterfaces)
						: _bannedTypesInfoCollector.GetTypeBannedApiInfos(typeSymbol, CheckInterfaces);

					if (bannedTypeInfos?.Count > 0)
					{
						var location = GetLocationFromNode(genericNameNode);
						ReportApiList(bannedTypeInfos, location);
					}
				}
				else if(GetBannedSymbolInfoForNonTypeSymbol(symbol) is Api bannedSymbolInfo)
				{
					var location = GetLocationFromNode(genericNameNode);
					ReportApi(bannedSymbolInfo, location);
				}

				Cancellation.ThrowIfCancellationRequested();
				base.VisitGenericName(genericNameNode);

				//----------------------------------------------------Local Function-----------------------------------------
				static Location GetLocationFromNode(GenericNameSyntax genericNameNode)
				{
					var typeName = genericNameNode.Identifier.ValueText;
					return !typeName.IsNullOrWhiteSpace()
						? (genericNameNode.Identifier.GetLocation() ?? genericNameNode.GetLocation())
						: genericNameNode.GetLocation();
				}
			}

			public override void VisitIdentifierName(IdentifierNameSyntax identifierNode)
			{
				Cancellation.ThrowIfCancellationRequested();

				if (SemanticModel.GetSymbolOrFirstCandidate(identifierNode, Cancellation) is not ISymbol symbol)
					return;

				Cancellation.ThrowIfCancellationRequested();
				CheckSymbolForBannedInfo(symbol, identifierNode);
			}

			private void CheckSymbolForBannedInfo(ISymbol symbol, SyntaxNode nodeToReport)
			{
				switch (symbol)
				{
					case ITypeParameterSymbol typeParameterSymbol:
						var bannedTypeParameterInfos = _bannedTypesInfoCollector.GetTypeParameterBannedApiInfos(typeParameterSymbol, CheckInterfaces);
						ReportApiList(bannedTypeParameterInfos, nodeToReport);
						return;

					case ITypeSymbol typeSymbol:
						var bannedTypeInfos = _bannedTypesInfoCollector.GetTypeBannedApiInfos(typeSymbol, CheckInterfaces);
						ReportApiList(bannedTypeInfos, nodeToReport);
						return;

					default:
						if (GetBannedSymbolInfoForNonTypeSymbol(symbol) is Api bannedSymbolInfo)
							ReportApi(bannedSymbolInfo, nodeToReport);

						return;
				}
			}

			private Api? GetBannedSymbolInfoForNonTypeSymbol(ISymbol nonTypeSymbol)
			{
				if (_apiBanInfoRetriever.GetInfoForApi(nonTypeSymbol) is Api bannedSymbolInfo)
					return bannedSymbolInfo;

				if (!nonTypeSymbol.IsOverride)
					return null;

				var overridesChain = nonTypeSymbol.GetOverridden();

				foreach (var overriden in overridesChain)
				{
					if (_apiBanInfoRetriever.GetInfoForApi(nonTypeSymbol) is Api bannedOverridenSymbolInfo)
						return bannedOverridenSymbolInfo;
				} 

				return null;
			}

			private void ReportApiList(List<Api>? bannedApisList, SyntaxNode node)
			{
				if (bannedApisList?.Count > 0)
					ReportApiList(bannedApisList, node.GetLocation());
			}

			private void ReportApiList(List<Api> bannedApisList, Location location)
			{
				foreach (Api bannedTypeInfo in bannedApisList)
					ReportApi(bannedTypeInfo, location);
			}

			private void ReportApi(in Api banApiInfo, SyntaxNode? node) =>
				ReportApi(banApiInfo, node?.GetLocation());

			private void ReportApi(in Api banApiInfo, Location? location)
			{
				var diagnosticDescriptor = GetDiagnosticFromBannedApiInfo(banApiInfo);

				if (diagnosticDescriptor == null)
					return;

				var diagnosticProperties = ImmutableDictionary<string, string>.Empty
																			  .Add(CommonConstants.ApiNameDiagnosticProperty, banApiInfo.FullName);
				var diagnostic = Diagnostic.Create(diagnosticDescriptor, location, diagnosticProperties!, banApiInfo.FullName);
				_syntaxContext.ReportDiagnosticWithSuppressionCheck(diagnostic);
			}

			private DiagnosticDescriptor? GetDiagnosticFromBannedApiInfo(in Api banApiInfo) => banApiInfo.ExtraInfo switch
			{
				ApiExtraInfo.None => Descriptors.CoreCompat1001_ApiNotPresentInDotNetCore,
				ApiExtraInfo.Obsolete 			  => Descriptors.CoreCompat1002_ApiObsoleteInDotNetCore,
				_ 								  => null
			};
		}
	}
}
