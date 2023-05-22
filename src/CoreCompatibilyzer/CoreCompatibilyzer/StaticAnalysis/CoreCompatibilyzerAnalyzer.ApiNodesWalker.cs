using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;

using CoreCompatibilyzer.BannedApiData.Model;
using CoreCompatibilyzer.BannedApiData.Storage;
using CoreCompatibilyzer.StaticAnalysis.BannedApiRetriever;
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
			private readonly IApiBanInfoRetriever _apiBanInfoRetriever;
			private readonly IBannedApiStorage _bannedApiStorage;

			private readonly BannedTypesInfoCollector _bannedTypesInfoCollector;

			private CancellationToken Cancellation => _syntaxContext.CancellationToken;

			private SemanticModel SemanticModel => _syntaxContext.SemanticModel;

            public ApiNodesWalker(SyntaxNodeAnalysisContext syntaxContext, IBannedApiStorage bannedApiStorage, IApiBanInfoRetriever apiBanInfoRetriever)
            {
                _syntaxContext 		 	  = syntaxContext;
				_bannedApiStorage 	 	  = bannedApiStorage;
				_apiBanInfoRetriever 	  = apiBanInfoRetriever;
				_bannedTypesInfoCollector = new BannedTypesInfoCollector(apiBanInfoRetriever, syntaxContext.CancellationToken);
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
						BannedApi? bannedNamespaceOrTypeInfo = _apiBanInfoRetriever.GetBanInfoForApi(namespaceSymbol);

						if (bannedNamespaceOrTypeInfo.HasValue)
							ReportApi(bannedNamespaceOrTypeInfo.Value, usingDirectiveNode.Name);

						break;
					
					case ITypeSymbol typeSymbol:
						var bannedTypeInfos = GetBannedInfosFromTypeSymbolAndItsHierarchy(typeSymbol);
						ReportApiList(bannedTypeInfos, usingDirectiveNode.Name);
						break;
				}	
			}

			public override void VisitGenericName(GenericNameSyntax genericNameNode)
			{
				Cancellation.ThrowIfCancellationRequested();

				if (SemanticModel.GetSymbolOrFirstCandidate(genericNameNode, Cancellation) is not ITypeSymbol typeSymbol)
				{
					Cancellation.ThrowIfCancellationRequested();
					base.VisitGenericName(genericNameNode);
					return;
				}

				Cancellation.ThrowIfCancellationRequested();

				var bannedTypeInfos = GetBannedInfosFromTypeSymbolAndItsHierarchy(typeSymbol);

				if (bannedTypeInfos?.Count > 0)
				{
					Location location = GetLocationFromNode(genericNameNode);
					ReportApiList(bannedTypeInfos, location);
				}


				if (_apiBanInfoRetriever.GetBanInfoForApi(typeSymbol) is BannedApi bannedTypeInfo)
				{
					
					ReportApi(bannedTypeInfo, location);
				}
				else if (typeSymbol.)

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

			public override void VisitQualifiedName(QualifiedNameSyntax qualifiedNameNode)
			{
				Cancellation.ThrowIfCancellationRequested();

				if (SemanticModel.GetSymbolOrFirstCandidate(qualifiedNameNode, Cancellation) is not ITypeSymbol typeSymbol)
					return;

				Cancellation.ThrowIfCancellationRequested();

				var bannedTypeInfos = GetBannedInfosFromTypeSymbolAndItsHierarchy(typeSymbol);
				ReportApiList(bannedTypeInfos, qualifiedNameNode);
			}

			public override void VisitIdentifierName(IdentifierNameSyntax identifierNode)
			{
				Cancellation.ThrowIfCancellationRequested();

				if (SemanticModel.GetSymbolOrFirstCandidate(identifierNode, Cancellation) is not ISymbol symbol)
					return;

				Cancellation.ThrowIfCancellationRequested();

				switch (symbol)
				{
					case ITypeParameterSymbol typeParameterSymbol:
						CheckTypeParameterConstraints(identifierNode, typeParameterSymbol);
						return;

					case ITypeSymbol typeSymbol:
						var bannedTypeInfos = _bannedTypesInfoCollector.GetTypeBannedApiInfos(typeSymbol);
						ReportApiList(bannedTypeInfos, identifierNode);
						return;
				}

				
			}

			private void ReportApiList(List<BannedApi>? bannedApisList, SyntaxNode node)
			{
				if (bannedApisList?.Count > 0)
					ReportApiList(bannedApisList, node.GetLocation());
			}

			private void ReportApiList(List<BannedApi> bannedApisList, Location location)
			{
				foreach (BannedApi bannedTypeInfo in bannedApisList)
					ReportApi(bannedTypeInfo, location);
			}

			private void ReportApi(in BannedApi banApiInfo, SyntaxNode? node) =>
				ReportApi(banApiInfo, node?.GetLocation());

			private void ReportApi(in BannedApi banApiInfo, Location? location)
			{
				var diagnosticDescriptor = GetDiagnosticFromBannedApiInfo(banApiInfo);

				if (diagnosticDescriptor == null)
					return;

				_syntaxContext.ReportDiagnosticWithSuppressionCheck(
						Diagnostic.Create(diagnosticDescriptor, location, banApiInfo.FullName));
			}

			private DiagnosticDescriptor? GetDiagnosticFromBannedApiInfo(in BannedApi banApiInfo) => banApiInfo.BannedApiType switch
			{
				BannedApiType.NotPresentInNetCore => Descriptors.CoreCompat1001_ApiNotPresentInDotNetCore,
				BannedApiType.Obsolete 			  => Descriptors.CoreCompat1002_ApiObsoleteInDotNetCore,
				_ 								  => null
			};
		}
	}
}
