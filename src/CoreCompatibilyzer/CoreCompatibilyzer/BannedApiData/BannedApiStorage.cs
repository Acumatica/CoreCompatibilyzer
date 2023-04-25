using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.BannedApiData
{
    /// <summary>
    /// A banned API storage.
    /// </summary>
    public class BannedApiStorage : IBannedApiStorage
	{
        private readonly IReadOnlyDictionary<ApiKind, IReadOnlyDictionary<string, BannedApi>> _bannedApisByDocIdGroupedByApiKind;
      
		public int BannedApiKindsCount => _bannedApisByDocIdGroupedByApiKind.Count;

        private BannedApiStorage()
        {
			_bannedApisByDocIdGroupedByApiKind = ImmutableDictionary<ApiKind, IReadOnlyDictionary<string, BannedApi>>.Empty;
		}

        private BannedApiStorage(IEnumerable<BannedApi> bannedApis)
        {
			_bannedApisByDocIdGroupedByApiKind = 
				bannedApis.GroupBy(api => api.Kind)
						  .ToDictionary(keySelector:	 groupedApi => groupedApi.Key,
										elementSelector: groupedApi => groupedApi.ToDictionary(api => api.DocID) as IReadOnlyDictionary<string, BannedApi>);
        }

		public static async Task<BannedApiStorage> InitializeAsync(IBannedApiDataProvider bannedApiDataProvider, CancellationToken cancellation)
		{
			bannedApiDataProvider.ThrowIfNull(nameof(bannedApiDataProvider));
			cancellation.ThrowIfCancellationRequested();

			var bannedApis =	await bannedApiDataProvider.GetBannedApiDataAsync(cancellation).ConfigureAwait(false);
			cancellation.ThrowIfCancellationRequested();

			return bannedApis == null 
				? new BannedApiStorage()
				: new BannedApiStorage(bannedApis);
		}

		public int CountOfBannedApis(ApiKind apiKind)
		{
			var bannedApiOfThisKind = BannedApiByKind(apiKind);
			return bannedApiOfThisKind?.Count ?? 0;
		}

		public BannedApi? GetBannedApi(ApiKind apiKind, string apiDocId)
		{
			var bannedApisOfThisKind = BannedApiByKind(apiKind);
			return bannedApisOfThisKind?.TryGetValue(apiDocId, out var bannedApi) == true
				? bannedApi
				: null;
		}

		public bool ContainsBannedApi(ApiKind apiKind, string apiDocId) =>
			BannedApiByKind(apiKind)?.ContainsKey(apiDocId) ?? false;

		private IReadOnlyDictionary<string, BannedApi>? BannedApiByKind(ApiKind apiKind) =>
			_bannedApisByDocIdGroupedByApiKind.TryGetValue(apiKind, out var api)
				? api
				: null;
	}
}
