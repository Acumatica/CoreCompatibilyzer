using System;
using System.Collections.Generic;
using System.Linq;

using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.ApiData.Model
{
	/// <summary>
	/// API search result in the banned API database.
	/// </summary>
	public readonly struct ApiSearchResult
	{
		public Api ClosestBannedApi { get; }

		public Api ApiFoundInDB { get; }

        public ApiSearchResult(Api closestBannedApi, Api apiFoundInDB)
        {
			ClosestBannedApi = closestBannedApi.ThrowIfNull(nameof(closestBannedApi));
			ApiFoundInDB	 = apiFoundInDB.ThrowIfNull(nameof(apiFoundInDB));
        }

		public void Deconstruct(out Api closestBannedApi, out Api apiFoundInDB)
		{
			closestBannedApi = ClosestBannedApi;
			apiFoundInDB 	 = ApiFoundInDB;
		}
    }
}
