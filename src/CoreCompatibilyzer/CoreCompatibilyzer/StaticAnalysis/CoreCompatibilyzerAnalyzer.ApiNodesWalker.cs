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

			private CancellationToken Cancellation => _syntaxContext.CancellationToken;

			private SemanticModel SemanticModel => _syntaxContext.SemanticModel;

            public ApiNodesWalker(SyntaxNodeAnalysisContext syntaxContext, IBannedApiStorage bannedApiStorage, IApiBanInfoRetriever apiBanInfoRetriever)
            {
                _syntaxContext 		 = syntaxContext;
				_bannedApiStorage 	 = bannedApiStorage;
				_apiBanInfoRetriever = apiBanInfoRetriever;
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

				BannedApi? bannedNamespaceOrTypeInfo = _apiBanInfoRetriever.GetBanInfoForApi(typeOrNamespaceSymbol);

				if (bannedNamespaceOrTypeInfo.HasValue)
					ReportApi(bannedNamespaceOrTypeInfo.Value, usingDirectiveNode.Name);
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

				if (genericNameNode.IsUnboundGenericName)
					typeSymbol = typeSymbol.OriginalDefinition;

				if (_apiBanInfoRetriever.GetBanInfoForApi(typeSymbol) is BannedApi bannedTypeInfo)
				{
					Location location = GetLocationFromNode(genericNameNode);
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

				if (_apiBanInfoRetriever.GetBanInfoForApi(typeSymbol) is BannedApi bannedTypeInfo)
					ReportApi(bannedTypeInfo, qualifiedNameNode);
			}

			public override void VisitIdentifierName(IdentifierNameSyntax identifierNode)
			{
				Cancellation.ThrowIfCancellationRequested();

				if (SemanticModel.GetSymbolOrFirstCandidate(identifierNode, Cancellation) is not ITypeSymbol typeSymbol)
					return;

				Cancellation.ThrowIfCancellationRequested();

				if (typeSymbol is ITypeParameterSymbol typeParameterSymbol)
				{
					CheckTypeParameterConstraints(identifierNode, typeParameterSymbol);
				}
				else if (_apiBanInfoRetriever.GetBanInfoForApi(typeSymbol) is BannedApi bannedTypeInfo)
				{
					ReportApi(bannedTypeInfo, identifierNode);
				}
			}

			private void CheckTypeParameterConstraints(IdentifierNameSyntax identifierNode, ITypeParameterSymbol typeParameterSymbol)
			{
				if (typeParameterSymbol.ConstraintTypes.IsDefaultOrEmpty)
					return;

				Location? location = null;

				foreach (INamedTypeSymbol constraintNamedType in typeParameterSymbol.ConstraintTypes.OfType<INamedTypeSymbol>())
				{
					var bannedTypeInfos = GetBannedInfoFromTypeSymbol(constraintNamedType);

					if (bannedTypeInfos?.Count is null or 0)
						continue;

					location ??= identifierNode.GetLocation();

					foreach (BannedApi bannedTypeInfo in bannedTypeInfos)
						ReportApi(bannedTypeInfo, location);
				}
			}

			private List<BannedApi>? GetBannedInfoFromTypeSymbol(ITypeSymbol typeSymbol)
			{
				List<BannedApi>? typeBannedApiInfos = null;

				if (_apiBanInfoRetriever.GetBanInfoForApi(typeSymbol) is BannedApi bannedTypeInfo)
				{
					typeBannedApiInfos ??= new List<BannedApi>(capacity: 2);
					typeBannedApiInfos.Add(bannedTypeInfo);
				}
					

				if (!typeSymbol.IsStatic && typeSymbol.TypeKind is TypeKind.Class or TypeKind.Interface)
				{
					foreach (var baseType in typeSymbol.GetBaseTypes())
					{
						if (baseType.SpecialType == SpecialType.System_Object)
							break;

						if (_apiBanInfoRetriever.GetBanInfoForApi(baseType) is BannedApi bannedBaseTypeInfo)
						{
							typeBannedApiInfos ??= new List<BannedApi>(capacity: 2);
							typeBannedApiInfos.Add(bannedBaseTypeInfo);
							
							// If we found something incompatible there is no need to go lower
							break;
						}
					}
				}

				var interfaces = typeSymbol.AllInterfaces;

				if (!interfaces.IsDefaultOrEmpty)
				{
					foreach (INamedTypeSymbol @interface in interfaces)
					{
						if (_apiBanInfoRetriever.GetBanInfoForApi(@interface) is BannedApi bannedInterfaceTypeInfo)
						{
							typeBannedApiInfos ??= new List<BannedApi>(capacity: 2);
							typeBannedApiInfos.Add(bannedInterfaceTypeInfo);

							// If we found something incompatible there is no need to go lower
							break;
						}
					}
				}

				return typeBannedApiInfos;
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
