using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using CoreCompatibilyzer.ApiData.Model;

namespace CoreCompatibilyzer.ApiData.Storage
{
	public partial class ApiStorage
	{
		/// <summary>
		/// A default API storage.
		/// </summary>
		private class DefaultApiStorage : IApiStorage
		{
			private readonly IReadOnlyDictionary<ApiKind, IReadOnlyDictionary<string, Api>> _ApisByDocIdGroupedByApiKind;

			public int ApiKindsCount => _ApisByDocIdGroupedByApiKind.Count;

			public DefaultApiStorage()
			{
				_ApisByDocIdGroupedByApiKind = ImmutableDictionary<ApiKind, IReadOnlyDictionary<string, Api>>.Empty;
			}

			public DefaultApiStorage(IEnumerable<Api> apis)
			{
				_ApisByDocIdGroupedByApiKind =
					apis.GroupBy(api => api.Kind)
						.ToDictionary(keySelector: groupedApi => groupedApi.Key,
									  elementSelector: GetApisOfSameKindByDocID);
			}

			private IReadOnlyDictionary<string, Api> GetApisOfSameKindByDocID(IEnumerable<Api> apis)
			{
				Dictionary<string, Api> apisByDocID = new();

				foreach (var api in apis)
				{
					if (apisByDocID.TryGetValue(api.DocID, out Api duplicateApi))
					{
						if (duplicateApi.ExtraInfo == ApiExtraInfo.Obsolete && api.ExtraInfo == ApiExtraInfo.None)
							apisByDocID[api.DocID] = api;
					}
					else
						apisByDocID.Add(api.DocID, api);
				}

				return apisByDocID;
			}

			public int CountOfApis(ApiKind apiKind)
			{
				var apiOfThisKind = ApiByKind(apiKind);
				return apiOfThisKind?.Count ?? 0;
			}

			public Api? GetApi(ApiKind apiKind, string apiDocId)
			{
				var apisOfThisKind = ApiByKind(apiKind);
				return apisOfThisKind?.TryGetValue(apiDocId, out var bannedApi) == true
					? bannedApi
					: null;
			}

			public bool ContainsApi(ApiKind apiKind, string apiDocId) =>
				ApiByKind(apiKind)?.ContainsKey(apiDocId) ?? false;

			private IReadOnlyDictionary<string, Api>? ApiByKind(ApiKind apiKind) =>
				_ApisByDocIdGroupedByApiKind.TryGetValue(apiKind, out var api)
					? api
					: null;
		}
	}
}