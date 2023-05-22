using System;
using System.Collections.Generic;

using CoreCompatibilyzer.Utils.Common;
using CoreCompatibilyzer.BannedApiData.Model;

using Microsoft.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CoreCompatibilyzer.StaticAnalysis.BannedApiRetriever
{
    /// <summary>
    /// A retriever of the ban API info that only searches for the ban information of the API itself, all containing APIs are not checked.
    /// </summary>
    public class ApiBanInfoRetrieverWithWeakCache : IApiBanInfoRetriever
	{
		private class CacheEntry
		{
			public BannedApi? BannedApi { get; set; }
		}

		private readonly ConditionalWeakTable<ISymbol, CacheEntry> _weakCache = new();
		private readonly IApiBanInfoRetriever _innerApiInfoRetriever;

		public ApiBanInfoRetrieverWithWeakCache(IApiBanInfoRetriever innerApiInfoRetriever)
        {
			_innerApiInfoRetriever = innerApiInfoRetriever.ThrowIfNull(nameof(innerApiInfoRetriever));
        }

		public BannedApi? GetBanInfoForApi(ISymbol apiSymbol)
		{
			apiSymbol.ThrowIfNull(nameof(apiSymbol));

			if (_weakCache.TryGetValue(apiSymbol, out CacheEntry cacheEntry))
				return cacheEntry.BannedApi;

			var bannedApiInfo 	 = _innerApiInfoRetriever.GetBanInfoForApi(apiSymbol);
			cacheEntry		  	 = _weakCache.GetOrCreateValue(apiSymbol);
			cacheEntry.BannedApi = bannedApiInfo;

			return bannedApiInfo;
		}
	}
}