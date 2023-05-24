using System;
using System.Collections.Generic;

using CoreCompatibilyzer.Utils.Common;
using CoreCompatibilyzer.BannedApiData.Model;
using CoreCompatibilyzer.BannedApiData.Storage;

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
			Api? directBanInfo = base.GetInfoForApiImpl(apiSymbol, apiKind);

			if (directBanInfo != null)
				return directBanInfo;

			// We just checked API for ban directly. There is no containing API that could be banned for namespaces and undefined APIs.
			if (apiKind is ApiKind.Namespace or ApiKind.Undefined)
				return null;

			Api? namespaceBanInfo = GetBanInfoForApiNamespace(apiSymbol.ContainingNamespace);

			if (namespaceBanInfo != null)
				return namespaceBanInfo;

			// We checked API for ban info directly and for banned namespaces. Non nested types don't have other parent APIs that could be banned
			if (apiSymbol is ITypeSymbol typeSymbol && typeSymbol.ContainingType == null) 
				return null;

			Api? typeBanInfo = GetBanInfoForContainingTypes(apiSymbol.ContainingType);

			if (typeBanInfo != null)
				return typeBanInfo;

			// We checked API directly and its containing namespace and types. 
			// Fields, events, properties and normal methods don't have other parent APIs that could be banned	
			if ((apiKind is ApiKind.Field or ApiKind.Property or ApiKind.Event) ||
				apiSymbol is not IMethodSymbol methodSymbol)
			{
				return null;
			}

			// The only API kind left to check are property and event accessors, since they can be banned via their corresponding property/event
			Api? accessorBanInfo = GetBanInfoForAccessorMethod(methodSymbol);
			return accessorBanInfo;
		}

		private Api? GetBanInfoForApiNamespace(INamespaceSymbol? apiNamespaceSymbol) =>
			apiNamespaceSymbol != null && !apiNamespaceSymbol.IsGlobalNamespace
				? GetInfoForSymbol(apiNamespaceSymbol, ApiKind.Namespace)
				: null;

		private Api? GetBanInfoForContainingTypes(INamedTypeSymbol? firstContainingType)
		{
			INamedTypeSymbol? currentType = firstContainingType;

			while (currentType != null) 
			{
				var typeBanInfo = GetInfoForSymbol(currentType, ApiKind.Type);

				if (typeBanInfo != null)
					return typeBanInfo;

				currentType = currentType.BaseType;
			}

			return null;
		}

		private Api? GetBanInfoForAccessorMethod(IMethodSymbol acessorMethod) =>
			acessorMethod.AssociatedSymbol switch
			{
				IPropertySymbol propertySymbol => GetInfoForSymbol(propertySymbol, ApiKind.Property),
				IEventSymbol eventSymbol       => GetInfoForSymbol(eventSymbol, ApiKind.Event),
				_                              => null
			};
	}
}