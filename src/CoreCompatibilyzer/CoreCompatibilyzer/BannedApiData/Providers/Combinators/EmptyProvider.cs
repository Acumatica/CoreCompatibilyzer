using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CoreCompatibilyzer.BannedApiData.Model;

namespace CoreCompatibilyzer.BannedApiData.Providers
{
	/// <summary>
	/// A data provider that always provides empty data.
	/// </summary>
	internal class EmptyProvider : IBannedApiDataProvider
	{
		private readonly Task<IEnumerable<BannedApi>?> _resultTask;

		/// <inheritdoc/>
		public bool IsDataAvailable { get; }

		public EmptyProvider(bool considerDataAvailable)
        {
			IsDataAvailable = considerDataAvailable;
			_resultTask = IsDataAvailable
				? Task.FromResult<IEnumerable<BannedApi>?>(Enumerable.Empty<BannedApi>())
				: Task.FromResult<IEnumerable<BannedApi>?>(null);
		}

		/// <inheritdoc/>
		public Task<IEnumerable<BannedApi>?> GetBannedApiDataAsync(CancellationToken cancellation) => 
			_resultTask;

		public IEnumerable<BannedApi>? GetBannedApiData(CancellationToken cancellation) =>
			IsDataAvailable
				? Enumerable.Empty<BannedApi>()
				: null;
	}
}
