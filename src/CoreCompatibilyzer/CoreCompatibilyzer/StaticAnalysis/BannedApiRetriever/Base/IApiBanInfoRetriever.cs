using System;
using System.Collections.Generic;

using CoreCompatibilyzer.BannedApiData;

using Microsoft.CodeAnalysis;

namespace CoreCompatibilyzer.StaticAnalysis.BannedApiRetriever
{
	/// <summary>
	/// Interface for a retriever of the ban info for APIs.
	/// </summary>
	public interface IApiBanInfoRetriever
	{
		/// <summary>
		/// Gets the ban information for API. Returns <c>null</c> if the API is not banned.
		/// </summary>
		/// <param name="apiSymbol">The API symbol to check.</param>
		/// <returns>
		/// Returns the ban information for API or <c>null</c> if the API is not banned.
		/// </returns>
		BannedApi? GetBanInfoForApi(ISymbol apiSymbol);
	}
}