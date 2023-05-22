using System;
using System.Collections.Generic;

using CoreCompatibilyzer.Utils.Common;
using CoreCompatibilyzer.BannedApiData.Model;
using CoreCompatibilyzer.BannedApiData.Storage;

using Microsoft.CodeAnalysis;

namespace CoreCompatibilyzer.StaticAnalysis.BannedApiRetriever
{
    /// <summary>
    /// A retriever of the ban API info that only searches for the ban information of the API itself, all containing APIs are not checked.
    /// </summary>
    public class DirectApiBanInfoRetriever : IApiBanInfoRetriever
	{
		protected IBannedApiStorage Storage { get; }

        public DirectApiBanInfoRetriever(IBannedApiStorage bannedApiStorage)
        {
			Storage = bannedApiStorage.ThrowIfNull(nameof(bannedApiStorage));
        }

		public BannedApi? GetBanInfoForApi(ISymbol apiSymbol)
		{
			ApiKind apiKind = apiSymbol.GetApiKind();
			BannedApi? directBanApiInfo = GetBanInfoForApiImpl(apiSymbol, apiKind);

			return directBanApiInfo;
		}

		protected virtual BannedApi? GetBanInfoForApiImpl(ISymbol apiSymbol, ApiKind apiKind) =>
			GetBanInfoForSymbol(apiSymbol, apiKind);

		protected BannedApi? GetBanInfoForSymbol(ISymbol symbol, ApiKind symbolKind)
		{
			string? symbolDocID = symbol.GetDocumentationCommentId();
			return symbolDocID.IsNullOrWhiteSpace()
				? null
				: Storage.GetBannedApi(symbolKind, symbolDocID);
		}
	}
}