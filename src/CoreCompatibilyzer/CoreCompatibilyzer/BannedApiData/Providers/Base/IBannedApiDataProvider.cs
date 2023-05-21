using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using CoreCompatibilyzer.BannedApiData.Model;

namespace CoreCompatibilyzer.BannedApiData.Providers
{
    public interface IBannedApiDataProvider
	{
		/// <summary>
		/// Gets a value indicating whether the provider's banned API data is available.
		/// </summary>
		/// <value>
		/// True if the banned API data is available, false if not.
		/// </value>
		bool IsDataAvailable { get; }

		/// <summary>
		/// Gets banned API data synchronously from the provider or <see langword="null"/> if the provider's banned API data is not available. <br/>
		/// On the latter case the <see cref="IsDataAvailable"/> flag value is <see langword="false"/>.
		/// </summary>
		/// <param name="cancellation">A token that allows processing to be cancelled.</param>
		/// <returns>
		/// The banned API data.
		/// </returns>
		IEnumerable<BannedApi>? GetBannedApiData(CancellationToken cancellation);

		/// <summary>
		/// Gets the banned API data asynchronously from the provider or <see langword="null"/> if the provider's banned API data is not available. <br/>
		/// On the latter case the <see cref="IsDataAvailable"/> flag value is <see langword="false"/>.
		/// </summary>
		/// <param name="cancellation">A token that allows processing to be cancelled.</param>
		/// <returns>
		/// The task with banned API data.
		/// </returns>
		Task<IEnumerable<BannedApi>?> GetBannedApiDataAsync(CancellationToken cancellation);
    }
}
