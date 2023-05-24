using System;
using System.Collections.Generic;

using CoreCompatibilyzer.Utils.Common;
using CoreCompatibilyzer.ApiData.Model;
using CoreCompatibilyzer.ApiData.Storage;

using Microsoft.CodeAnalysis;

namespace CoreCompatibilyzer.StaticAnalysis.ApiInfoRetrievers
{
    /// <summary>
    /// A retriever of the ban API info that also checks for banned containing APIs.
    /// </summary>
    public class HierarchicalApiBanInfoRetriever : DirectApiInfoRetriever
	{
        public HierarchicalApiBanInfoRetriever(IApiStorage bannedApiStorage) : base(bannedApiStorage)
        { }

		protected override Api? GetInfoForApiImpl(ISymbol apiSymbol, ApiKind apiKind)
		{
			Api? directInfo = base.GetInfoForApiImpl(apiSymbol, apiKind);

			if (directInfo != null)
				return directInfo;

			// We just checked API for directly. There is no containing API for namespaces and undefined APIs.
			if (apiKind is ApiKind.Namespace or ApiKind.Undefined)
				return null;

			Api? namespaceInfo = GetInfoForApiNamespace(apiSymbol.ContainingNamespace);

			if (namespaceInfo != null)
				return namespaceInfo;

			// We checked API for info directly and for namespaces. Non nested types don't have other parent APIs
			if (apiSymbol is ITypeSymbol typeSymbol && typeSymbol.ContainingType == null) 
				return null;

			Api? typeInfo = GetInfoForContainingTypes(apiSymbol.ContainingType);

			if (typeInfo != null)
				return typeInfo;

			// We checked API directly and its containing namespace and types. 
			// Fields, events, properties and normal methods don't have other parent APIs
			if ((apiKind is ApiKind.Field or ApiKind.Property or ApiKind.Event) ||
				apiSymbol is not IMethodSymbol methodSymbol)
			{
				return null;
			}

			// The only API kind left to check are property and event accessors, since they are contained inside their corresponding property/event
			Api? accessorBanInfo = GetInfoForAccessorMethod(methodSymbol);
			return accessorBanInfo;
		}

		private Api? GetInfoForApiNamespace(INamespaceSymbol? apiNamespaceSymbol) =>
			apiNamespaceSymbol != null && !apiNamespaceSymbol.IsGlobalNamespace
				? GetInfoForSymbol(apiNamespaceSymbol, ApiKind.Namespace)
				: null;

		private Api? GetInfoForContainingTypes(INamedTypeSymbol? firstContainingType)
		{
			INamedTypeSymbol? currentType = firstContainingType;

			while (currentType != null) 
			{
				var typeInfo = GetInfoForSymbol(currentType, ApiKind.Type);

				if (typeInfo != null)
					return typeInfo;

				currentType = currentType.ContainingType;
			}

			return null;
		}

		private Api? GetInfoForAccessorMethod(IMethodSymbol acessorMethod) =>
			acessorMethod.AssociatedSymbol switch
			{
				IPropertySymbol propertySymbol => GetInfoForSymbol(propertySymbol, ApiKind.Property),
				IEventSymbol eventSymbol       => GetInfoForSymbol(eventSymbol, ApiKind.Event),
				_                              => null
			};
	}
}