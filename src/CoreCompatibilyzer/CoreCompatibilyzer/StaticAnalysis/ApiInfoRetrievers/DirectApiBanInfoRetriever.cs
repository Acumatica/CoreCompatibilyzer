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
    /// A retriever of the ban API info that only searches for the ban information of the API itself, all containing APIs are not checked.
    /// </summary>
    public class DirectApiBanInfoRetriever : IApiInfoRetriever
	{
		protected IApiStorage Storage { get; }

        public DirectApiBanInfoRetriever(IApiStorage bannedApiStorage)
        {
			Storage = bannedApiStorage.ThrowIfNull(nameof(bannedApiStorage));
        }

		public Api? GetInfoForApi(ISymbol apiSymbol)
		{
			ApiKind apiKind = apiSymbol.GetApiKind();
			Api? directBanApiInfo = GetBanInfoForApiImpl(apiSymbol, apiKind);

			return directBanApiInfo;
		}

		protected virtual Api? GetBanInfoForApiImpl(ISymbol apiSymbol, ApiKind apiKind) =>
			GetBanInfoForSymbol(apiSymbol, apiKind);

		protected Api? GetBanInfoForSymbol(ISymbol symbol, ApiKind symbolKind)
		{
			string? symbolDocID = symbol.GetDocID();
			return symbolDocID.IsNullOrWhiteSpace()
				? null
				: Storage.GetApi(symbolKind, symbolDocID);
		}
	}
}