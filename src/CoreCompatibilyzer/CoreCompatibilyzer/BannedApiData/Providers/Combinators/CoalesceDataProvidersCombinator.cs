using System;
using System.Collections.Generic;
using System.Linq;
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
		/// <returns>
		/// The task with banned API data.
		/// </returns>
		public async Task<IEnumerable<BannedApi>?> GetBannedApiDataAsync()
		{
            foreach (var provider in _providers)
            {
				if (!provider.IsDataAvailable) 
					continue;

				var bannedApiData = await provider.GetBannedApiDataAsync().ConfigureAwait(false);

				if (bannedApiData != null)
					return bannedApiData;
            }

			return null;
		}
	}
}
