using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.BannedApiData
{
    public class DataProvidersCoalesceCombinator : IBannedApiDataProvider
	{
		/// <summary>
		/// The providers to be combined. 
		/// Providers are ordered in the order they are passed to the combinator provider.
		/// </summary>
		private readonly IEnumerable<IBannedApiDataProvider> _providers;

		public bool IsDataAvailable => _providers.Any(p => p.IsDataAvailable);

		public DataProvidersCoalesceCombinator(IEnumerable<IBannedApiDataProvider> providers)
        {
			_providers = providers.ThrowIfNull(nameof(providers));
        }

		/// <summary>
		/// Gets the banned API data asynchronously from the provider or <see langword="null"/> if the provider's banned API data is not available. <br/>
		/// On the latter case the <see cref="IsDataAvailable"/> flag value is <see langword="false"/>.
		/// </summary>
		/// <param name="cancellation">A token that allows processing to be cancelled.</param>
		/// <returns>
		/// The task with banned API data.
		/// </returns>
		public async Task<IEnumerable<BannedApi>?> GetBannedApiDataAsync(CancellationToken cancellation)
		{
            foreach (var provider in _providers)
            {
				cancellation.ThrowIfCancellationRequested();

				if (!provider.IsDataAvailable) 
					continue;

				var bannedApiData = await provider.GetBannedApiDataAsync(cancellation).ConfigureAwait(false);
				cancellation.ThrowIfCancellationRequested();

				if (bannedApiData != null)
					return bannedApiData;
            }

			return null;
		}
	}
}
