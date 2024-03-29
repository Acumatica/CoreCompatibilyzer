﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

using CoreCompatibilyzer.ApiData.Model;
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
			private readonly IApiInfoRetriever? _whiteListInfoRetriever;
			private readonly BannedTypesInfoCollector _bannedTypesInfoCollector;

			private readonly HashSet<string> _namespacesWithUsedWhiteListedMembers = new();
			private readonly List<(UsingDirectiveSyntax Using, INamespaceSymbol Namespace, ApiSearchResult BanApiInfo)> _suspiciousUsings = new();

			private readonly HashSet<(Location ErrorLocation, Api ErrorInfo)> _reportedErrors = new();

			public bool CheckInterfaces { get; }

			private CancellationToken Cancellation => _syntaxContext.CancellationToken;

			private SemanticModel SemanticModel => _syntaxContext.SemanticModel;

            public ApiNodesWalker(SyntaxNodeAnalysisContext syntaxContext, IApiInfoRetriever apiBanInfoRetriever,
								  IApiInfoRetriever? whiteListInfoRetriever, bool checkInterfaces)
            {
                _syntaxContext 		 	  = syntaxContext;
				_apiBanInfoRetriever 	  = apiBanInfoRetriever;
				_whiteListInfoRetriever   = whiteListInfoRetriever;
				_bannedTypesInfoCollector = new BannedTypesInfoCollector(apiBanInfoRetriever, whiteListInfoRetriever, syntaxContext.CancellationToken);
				CheckInterfaces 		  = checkInterfaces;
			}

			public void CheckSyntaxTree(CompilationUnitSyntax root)
			{
				root.Accept(this);

				if (_suspiciousUsings.Count == 0)
					return;

				if (_bannedTypesInfoCollector.NamespacesWithUsedWhiteListedMembers.Count > 0)
					_namespacesWithUsedWhiteListedMembers.AddRange(_bannedTypesInfoCollector.NamespacesWithUsedWhiteListedMembers);

				var usingsToReport = _namespacesWithUsedWhiteListedMembers.Count > 0
					? _suspiciousUsings.Where(usingInfo => !_namespacesWithUsedWhiteListedMembers.Contains(usingInfo.Namespace.ToString()))
					: _suspiciousUsings;

				foreach (var (@using, @namespace, banInfo) in usingsToReport)
				{
					ReportApi(@namespace, banInfo, @using.Name);
				}
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
						if (_apiBanInfoRetriever.GetInfoForApi(namespaceSymbol) is ApiSearchResult bannedNamespaceInfo)
							_suspiciousUsings.Add((usingDirectiveNode, namespaceSymbol, bannedNamespaceInfo));

						break;
					
					case ITypeSymbol typeSymbol:
						var bannedTypeInfos = _bannedTypesInfoCollector.GetTypeBannedApiInfos(typeSymbol, CheckInterfaces);
						ReportApiList(typeSymbol, bannedTypeInfos, usingDirectiveNode.Name);
						break;
				}	
			}

			/// <summary>
			/// Skip visit of the namespace declaration name.
			/// </summary>
			/// <param name="namespaceDeclaration">The namespace declaration.</param>
			public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax namespaceDeclaration)
			{
				Cancellation.ThrowIfCancellationRequested();

				foreach (var attributeList in namespaceDeclaration.AttributeLists)
					attributeList.Accept(this);

				foreach (var @using in namespaceDeclaration.Usings)
					@using.Accept(this);

				foreach (var @extern in namespaceDeclaration.Externs)
					@extern.Accept(this);

				foreach (var member in namespaceDeclaration.Members)
					member.Accept(this);
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
					List<ApiSearchResult>? bannedTypeInfos = typeSymbol is ITypeParameterSymbol typeParameterSymbol
						? _bannedTypesInfoCollector.GetTypeParameterBannedApiInfos(typeParameterSymbol, CheckInterfaces)
						: _bannedTypesInfoCollector.GetTypeBannedApiInfos(typeSymbol, CheckInterfaces);

					if (bannedTypeInfos?.Count > 0)
					{
						var location = GetLocationFromNode(genericNameNode);
						ReportApiList(typeSymbol, bannedTypeInfos, location);
					}
				}
				else if(GetBannedSymbolInfoForNonTypeSymbol(symbol) is ApiSearchResult bannedSymbolInfo)
				{
					var location = GetLocationFromNode(genericNameNode);
					ReportApi(symbol, bannedSymbolInfo, location, checkWhiteList: true);
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

			public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax memberAccessExpression)
			{
				Cancellation.ThrowIfCancellationRequested();

				if (SemanticModel.GetSymbolOrFirstCandidate(memberAccessExpression, Cancellation) is not ISymbol symbol)
				{
					base.VisitMemberAccessExpression(memberAccessExpression);
					return;
				}

				Cancellation.ThrowIfCancellationRequested();
				CheckSymbolForBannedInfo(symbol, memberAccessExpression.Name);
			}

			public override void VisitIdentifierName(IdentifierNameSyntax identifierNode)
			{
				Cancellation.ThrowIfCancellationRequested();

				if (SemanticModel.GetSymbolOrFirstCandidate(identifierNode, Cancellation) is not ISymbol symbol)
					return;

				Cancellation.ThrowIfCancellationRequested();
				CheckSymbolForBannedInfo(symbol, identifierNode);
			}

			public override void VisitQualifiedName(QualifiedNameSyntax qualifiedName)
			{
				Cancellation.ThrowIfCancellationRequested();

				if (SemanticModel.GetSymbolOrFirstCandidate(qualifiedName, Cancellation) is not ISymbol symbol)
				{
					base.VisitQualifiedName(qualifiedName);
					return;
				}

				Cancellation.ThrowIfCancellationRequested();
				CheckSymbolForBannedInfo(symbol, qualifiedName.Right);
			}

			private void CheckSymbolForBannedInfo(ISymbol symbol, SyntaxNode nodeToReport)
			{
				switch (symbol)
				{
					case ITypeParameterSymbol typeParameterSymbol:
						var bannedTypeParameterInfos = _bannedTypesInfoCollector.GetTypeParameterBannedApiInfos(typeParameterSymbol, CheckInterfaces);
						ReportApiList(typeParameterSymbol, bannedTypeParameterInfos, nodeToReport);
						return;

					case ITypeSymbol typeSymbol:
						var bannedTypeInfos = _bannedTypesInfoCollector.GetTypeBannedApiInfos(typeSymbol, CheckInterfaces);
						ReportApiList(typeSymbol, bannedTypeInfos, nodeToReport);
						return;

					default:
						if (GetBannedSymbolInfoForNonTypeSymbol(symbol) is ApiSearchResult bannedSymbolInfo)
							ReportApi(symbol, bannedSymbolInfo, nodeToReport);

						return;
				}
			}

			private ApiSearchResult? GetBannedSymbolInfoForNonTypeSymbol(ISymbol nonTypeSymbol)
			{
				if (_apiBanInfoRetriever.GetInfoForApi(nonTypeSymbol) is ApiSearchResult bannedSymbolInfo)
					return bannedSymbolInfo;

				if (!nonTypeSymbol.IsOverride)
					return null;

				var overridesChain = nonTypeSymbol.GetOverridden();

				foreach (var overridenSymbol in overridesChain)
				{
					if (_apiBanInfoRetriever.GetInfoForApi(overridenSymbol) is ApiSearchResult bannedOverridenSymbolInfo)
						return bannedOverridenSymbolInfo;
				} 

				return null;
			}

			private void ReportApiList(ISymbol symbolToReport, List<ApiSearchResult>? bannedApisList, SyntaxNode node)
			{
				if (bannedApisList?.Count > 0)
					ReportApiList(symbolToReport, bannedApisList, node.GetLocation());
			}

			private void ReportApiList(ISymbol symbolToReport, List<ApiSearchResult> bannedApisList, Location location)
			{
				if (IsInWhiteList(symbolToReport))
					return;

				foreach (ApiSearchResult bannedApiInfo in bannedApisList)
					ReportApi(symbolToReport, bannedApiInfo, location, checkWhiteList: false);
			}

			private void ReportApi(ISymbol symbolToReport, ApiSearchResult banApiInfo, SyntaxNode? node) =>
				ReportApi(symbolToReport, banApiInfo, node?.GetLocation(), checkWhiteList: true);

			private void ReportApi(ISymbol symbolToReport, ApiSearchResult banApiInfo, Location? location, bool checkWhiteList)
			{
				if (checkWhiteList && IsInWhiteList(symbolToReport))
					return;

				if (location != null && !_reportedErrors.Add((location, banApiInfo.ClosestBannedApi)!))
					return;

				var diagnosticDescriptor = GetDiagnosticFromBannedApiInfo(banApiInfo.ApiFoundInDB.ExtraInfo);
				var diagnosticProperties = new Dictionary<string, string>
				{
					{ CommonConstants.ClosestBannedApiProperty, banApiInfo.ClosestBannedApi.RawApiData },
					{ CommonConstants.ApiFoundInDbProperty,		banApiInfo.ApiFoundInDB.RawApiData },
				}
				.ToImmutableDictionary();

				var diagnostic = Diagnostic.Create(diagnosticDescriptor, location, diagnosticProperties!, banApiInfo.ClosestBannedApi.FullName);
				_syntaxContext.ReportDiagnosticWithSuppressionCheck(diagnostic);
			}

			private bool IsInWhiteList(ISymbol symbol)
			{
				if (_whiteListInfoRetriever?.GetInfoForApi(symbol) is ApiSearchResult)
				{
					if (symbol.ContainingNamespace != null && !symbol.ContainingNamespace.IsGlobalNamespace)
						_namespacesWithUsedWhiteListedMembers.Add(symbol.ContainingNamespace.ToString());

					return true;
				}

				return false;
			}

			private DiagnosticDescriptor GetDiagnosticFromBannedApiInfo(ApiExtraInfo apiExtraInfo) => apiExtraInfo switch
			{
				ApiExtraInfo.None	  => Descriptors.CoreCompat1001_ApiNotPresentInDotNetCore,
				ApiExtraInfo.Obsolete => Descriptors.CoreCompat1002_ApiObsoleteInDotNetCore,
				_ 					  => throw new NotSupportedException($"Value \"{apiExtraInfo}\" of {nameof(ApiExtraInfo)} is not supported")
			};
		}
	}
}
