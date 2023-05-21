using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CoreCompatibilyzer.Utils.Common;
using CoreCompatibilyzer.BannedApiData.Model;

namespace CoreCompatibilyzer.BannedApiData.Providers
{
	/// <summary>
	/// A data providers coalesce combinator. Gets data sequentially from a list of providers until the first successful retrieval.
	/// </summary>
	public class DataProvidersCoalesceCombinator : IBannedApiDataProvider
	{
		/// <summary>
		/// The providers to be combined. 
		/// Providers are ordered in the order they are passed to the combinator provider.
		/// </summary>
		private readonly IEnumerable<IBannedApiDataProvider> _providers;

		/// <inheritdoc/>
		public bool IsDataAvailable => _providers.Any(p => p.IsDataAvailable);

		public DataProvidersCoalesceCombinator(IEnumerable<IBannedApiDataProvider> providers)
        {
			_providers = providers.ThrowIfNull(nameof(providers));
        }

		/// <inheritdoc/>
		public async Task<IEnumerable<BannedApi>?> GetBannedApiDataAsync(CancellationToken cancellation)
		{
            foreach (var provider in _providers)
            {
				cancellation.ThrowIfCancellationRequested();

				if (!provider.IsDataAvailable) 
					continue;

				var bannedApiData = await provider.GetBannedApiDataAsync(cancellation)
												  .WithCancellation(cancellation)
												  .ConfigureAwait(false);

				cancellation.ThrowIfCancellationRequested();

				if (bannedApiData != null)
					return bannedApiData;
            }

			return null;
		}

		public IEnumerable<BannedApi>? GetBannedApiData(CancellationToken cancellation)
		{
			foreach (var provider in _providers)
			{
				cancellation.ThrowIfCancellationRequested();

				if (!provider.IsDataAvailable)
					continue;

				var bannedApiData = provider.GetBannedApiData(cancellation);

				cancellation.ThrowIfCancellationRequested();

				if (bannedApiData != null)
					return bannedApiData;
			}

			return null;
		}
	}
}
