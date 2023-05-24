using System;
using System.Collections.Generic;

using CoreCompatibilyzer.Utils.Common;
using CoreCompatibilyzer.BannedApiData.Model;

using Microsoft.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CoreCompatibilyzer.StaticAnalysis.ApiInfoRetrievers
{
    /// <summary>
    /// A retriever of the API info that caches the result of another retriever.
    /// </summary>
    public class ApiInfoRetrieverWithWeakCache : IApiInfoRetriever
	{
		private class CacheEntry
		{
			public Api? BannedApi { get; set; }
		}

		private readonly ConditionalWeakTable<ISymbol, CacheEntry> _weakCache = new();
		private readonly IApiInfoRetriever _innerApiInfoRetriever;

		public ApiInfoRetrieverWithWeakCache(IApiInfoRetriever innerApiInfoRetriever)
        {
			_innerApiInfoRetriever = innerApiInfoRetriever.ThrowIfNull(nameof(innerApiInfoRetriever));
        }

		public Api? GetInfoForApi(ISymbol apiSymbol)
		{
			apiSymbol.ThrowIfNull(nameof(apiSymbol));

			if (_weakCache.TryGetValue(apiSymbol, out CacheEntry cacheEntry))
				return cacheEntry.BannedApi;

			var bannedApiInfo 	 = _innerApiInfoRetriever.GetInfoForApi(apiSymbol);
			cacheEntry		  	 = _weakCache.GetOrCreateValue(apiSymbol);
			cacheEntry.BannedApi = bannedApiInfo;

			return bannedApiInfo;
		}
	}
}