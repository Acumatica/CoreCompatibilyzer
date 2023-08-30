﻿using System;
using System.Collections.Generic;

using CoreCompatibilyzer.ApiData.Model;

using Microsoft.CodeAnalysis;

namespace CoreCompatibilyzer.StaticAnalysis.ApiInfoRetrievers
{
	/// <summary>
	/// Interface for a retriever of info for APIs.
	/// </summary>
	public interface IApiInfoRetriever
	{
		/// <summary>
		/// Gets the information for API. Returns <c>null</c> if the API is not found.
		/// </summary>
		/// <param name="apiSymbol">The API symbol to check.</param>
		/// <returns>
		/// Returns the information about banned API or <c>null</c> if the API is not found.
		/// </returns>
		ApiSearchResult? GetInfoForApi(ISymbol apiSymbol);
	}
}