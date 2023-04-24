using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace CoreCompatibilyzer.BannedApiData
{
    /// <summary>
    /// An interface for the banned API storage.
    /// </summary>
    public interface IBannedApiStorage
	{
		public int BannedApiKindsCount { get; }

		/// <summary>
		/// Count of banned APIs of the <paramref name="apiKind"/> kind.
		/// </summary>
		/// <param name="apiKind">The API kind.</param>
		/// <returns>
		/// The number of banned APIs of the <paramref name="apiKind"/> kind.
		/// </returns>
		public int CountOfBannedApis(ApiKind apiKind);

		/// <summary>
		/// Gets the banned API or null if there is no such API in the storage.
		/// </summary>
		/// <param name="apiKind">The API kind.</param>
		/// <param name="fullApiName">The full API name.</param>
		/// <returns>
		/// The banned API or null.
		/// </returns>
		public BannedApi? GetBannedApi(ApiKind apiKind, string fullApiName);

		/// <summary>
		/// Gets the banned API or null if there is no such API in the storage.
		/// </summary>
		/// <param name="apiSymbol">The API symbol.</param>
		/// <returns>
		/// The banned API or null.
		/// </returns>
		public BannedApi? GetBannedApi(ISymbol apiSymbol);

		/// <summary>
		/// Query if the storage contains the banned API.
		/// </summary>
		/// <param name="apiKind">The API kind.</param>
		/// <param name="fullApiName">The full API name.</param>
		/// <returns>
		/// True if the storage contains the banned API, false if not.
		/// </returns>
		public bool ContainsBannedApi(ApiKind apiKind, string fullApiName);

		/// <summary>
		/// Query if the storage contains the banned API.
		/// </summary>
		/// <param name="apiSymbol">The API symbol.</param>
		/// <returns>
		/// True if the storage contains the banned API, false if not.
		/// </returns>
		public bool ContainsBannedApi(ISymbol apiSymbol);
	}
}
