using System;
using System.Collections.Generic;

using CoreCompatibilyzer.Utils.Common;
using CoreCompatibilyzer.ApiData.Model;
using CoreCompatibilyzer.ApiData.Storage;

using Microsoft.CodeAnalysis;

namespace CoreCompatibilyzer.StaticAnalysis.ApiInfoRetrievers
{
    /// <summary>
    /// A retriever of the API info that only searches for the information of the API itself, all containing APIs are not checked.
    /// </summary>
    public class DirectApiInfoRetriever : IApiInfoRetriever
	{
		protected IApiStorage Storage { get; }

        public DirectApiInfoRetriever(IApiStorage apiStorage)
        {
			Storage = apiStorage.ThrowIfNull(nameof(apiStorage));
        }

		public ApiSearchResult? GetInfoForApi(ISymbol apiSymbol)
		{
			ApiKind apiKind = apiSymbol.GetApiKind();
			ApiSearchResult? directApiInfo = GetInfoForApiImpl(apiSymbol, apiKind);

			return directApiInfo;
		}

		protected virtual ApiSearchResult? GetInfoForApiImpl(ISymbol apiSymbol, ApiKind apiKind)
		{
			var apiSymbolFoundInDb = GetInfoForSymbol(apiSymbol, apiKind);
			return apiSymbolFoundInDb != null
				? new ApiSearchResult(closestBannedApi: apiSymbolFoundInDb, apiSymbolFoundInDb)
				: null;
		}

		protected Api? GetInfoForSymbol(ISymbol symbol, ApiKind symbolKind) =>
			symbolKind switch
			{
				ApiKind.Method => GetInfoForMethodSymbol(symbol as IMethodSymbol),
				ApiKind.Type   => GetInfoForTypeSymbol(symbol as INamedTypeSymbol),
				_ 			   => GetInfoForRegularSymbol(symbol, symbolKind)
			};

		protected Api? GetInfoForMethodSymbol(IMethodSymbol? method)
		{
			if (method == null)
				return null;

			if (method.MethodKind == MethodKind.ReducedExtension && method.ReducedFrom != null)
			{
				var apiInfoForOriginalExtensionMethod = GetInfoForRegularSymbol(method.ReducedFrom, ApiKind.Method);

				if (apiInfoForOriginalExtensionMethod != null)
					return apiInfoForOriginalExtensionMethod;
			}

			if (method.OriginalDefinition != null && !SymbolEqualityComparer.Default.Equals(method, method.OriginalDefinition))
			{
				var apiInfoForOriginalGenericMethod = GetInfoForRegularSymbol(method.OriginalDefinition, ApiKind.Method);

				if (apiInfoForOriginalGenericMethod != null)
					return apiInfoForOriginalGenericMethod;
			}

			return GetInfoForRegularSymbol(method, ApiKind.Method);
		}

		protected Api? GetInfoForTypeSymbol(INamedTypeSymbol? type)
		{
			if (type == null)
				return null;

			if (type.IsGenericType && type.OriginalDefinition != null)
			{
				var apiInfoForOriginalGenericType = GetInfoForRegularSymbol(type.OriginalDefinition, ApiKind.Type);

				if (apiInfoForOriginalGenericType != null)
					return apiInfoForOriginalGenericType;
			}

			return GetInfoForRegularSymbol(type, ApiKind.Type);
		}

		protected Api? GetInfoForRegularSymbol(ISymbol symbol, ApiKind symbolKind)
		{
			string? symbolDocID = symbol.GetDocumentationCommentId().NullIfWhiteSpace();
			return symbolDocID.IsNullOrWhiteSpace()
				? null
				: Storage.GetApi(symbolKind, symbolDocID);
		}
	}
}