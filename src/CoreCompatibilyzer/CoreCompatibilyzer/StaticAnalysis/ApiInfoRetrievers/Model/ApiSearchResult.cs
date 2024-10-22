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
		public string ClosestBannedApiSymbolName { get; }

		public string ApiFoundInDbSymbolName { get; }

		public Api ClosestBannedApi { get; }

		public Api ApiFoundInDB { get; }

		public ApiSearchResult(Api closestBannedApi, Api apiFoundInDB, string closestBannedApiSymbolName, string apiFoundInDbSymbolName)
		{
			ClosestBannedApi 		   = closestBannedApi.ThrowIfNull(nameof(closestBannedApi));
			ApiFoundInDB 			   = apiFoundInDB.ThrowIfNull(nameof(apiFoundInDB));
			ClosestBannedApiSymbolName = closestBannedApiSymbolName.ThrowIfNullOrWhiteSpace();
			ApiFoundInDbSymbolName 	   = apiFoundInDbSymbolName.ThrowIfNullOrWhiteSpace();
		}
	}
}
