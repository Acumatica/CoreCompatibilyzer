using System;
using System.Collections.Generic;

using CoreCompatibilyzer.Utils.Common;
using CoreCompatibilyzer.BannedApiData.Model;
using CoreCompatibilyzer.BannedApiData.Storage;

using Microsoft.CodeAnalysis;
using CoreCompatibilyzer.Utils.Roslyn.Semantic;

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

		public Api? GetInfoForApi(ISymbol apiSymbol)
		{
			ApiKind apiKind = apiSymbol.GetApiKind();
			Api? directApiInfo = GetInfoForApiImpl(apiSymbol, apiKind);

			return directApiInfo;
		}

		protected virtual Api? GetInfoForApiImpl(ISymbol apiSymbol, ApiKind apiKind) =>
			GetInfoForSymbol(apiSymbol, apiKind);

		protected Api? GetInfoForSymbol(ISymbol symbol, ApiKind symbolKind)
		{
			string? symbolDocID = symbol.GetDocID();
			return symbolDocID.IsNullOrWhiteSpace()
				? null
				: Storage.GetApi(symbolKind, symbolDocID);
		}
	}
}