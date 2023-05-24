using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using CoreCompatibilyzer.BannedApiData.Model;

namespace CoreCompatibilyzer.BannedApiData.Storage
{
    public partial class BannedApiStorage
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
                                            elementSelector: GetBannedApisOfSameKindByDocID);
            }

            private IReadOnlyDictionary<string, BannedApi> GetBannedApisOfSameKindByDocID(IEnumerable<BannedApi> bannedApis) 
            {
                Dictionary<string, BannedApi> bannedApisByDocID = new();

                foreach (var api in bannedApis) 
                {
                    if (bannedApisByDocID.TryGetValue(api.DocID, out BannedApi duplicateApi))
                    {
                        if (duplicateApi.BannedApiType == BannedApiType.Obsolete && api.BannedApiType == BannedApiType.NotPresentInNetCore)
                            bannedApisByDocID[api.DocID] = api;
                    }
                    else
                        bannedApisByDocID.Add(api.DocID, api);
                }

                return bannedApisByDocID;
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