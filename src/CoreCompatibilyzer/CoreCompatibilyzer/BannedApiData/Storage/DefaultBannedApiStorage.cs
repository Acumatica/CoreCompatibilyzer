using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using CoreCompatibilyzer.BannedApiData.Model;

namespace CoreCompatibilyzer.BannedApiData.Storage
{
    public static partial class BannedApiStorage
    {
        /// <summary>
        /// A default banned API storage.
        /// </summary>
        private class DefaultBannedApiStorage : IBannedApiStorage
        {
            private readonly IReadOnlyDictionary<ApiKind, IReadOnlyDictionary<string, BannedApi>> _bannedApisByDocIdGroupedByApiKind;

            public int BannedApiKindsCount => _bannedApisByDocIdGroupedByApiKind.Count;

            public DefaultBannedApiStorage()
            {
                _bannedApisByDocIdGroupedByApiKind = ImmutableDictionary<ApiKind, IReadOnlyDictionary<string, BannedApi>>.Empty;
            }

            public DefaultBannedApiStorage(IEnumerable<BannedApi> bannedApis)
            {
                _bannedApisByDocIdGroupedByApiKind =
                    bannedApis.GroupBy(api => api.Kind)
                              .ToDictionary(keySelector: groupedApi => groupedApi.Key,
                                            elementSelector: groupedApi => groupedApi.ToDictionary(api => api.DocID) as IReadOnlyDictionary<string, BannedApi>);
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
}