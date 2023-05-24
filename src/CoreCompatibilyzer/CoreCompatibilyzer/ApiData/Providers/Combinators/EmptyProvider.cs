using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CoreCompatibilyzer.ApiData.Model;

namespace CoreCompatibilyzer.ApiData.Providers
{
	/// <summary>
	/// A data provider that always provides empty data.
	/// </summary>
	internal class EmptyProvider : IBannedApiDataProvider
	{
		private readonly Task<IEnumerable<Api>?> _resultTask;

		/// <inheritdoc/>
		public bool IsDataAvailable { get; }

		public EmptyProvider(bool considerDataAvailable)
        {
			IsDataAvailable = considerDataAvailable;
			_resultTask = IsDataAvailable
				? Task.FromResult<IEnumerable<Api>?>(Enumerable.Empty<Api>())
				: Task.FromResult<IEnumerable<Api>?>(null);
		}

		/// <inheritdoc/>
		public Task<IEnumerable<Api>?> GetBannedApiDataAsync(CancellationToken cancellation) => 
			_resultTask;

		public IEnumerable<Api>? GetBannedApiData(CancellationToken cancellation) =>
			IsDataAvailable
				? Enumerable.Empty<Api>()
				: null;
	}
}
