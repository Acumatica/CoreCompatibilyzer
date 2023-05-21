using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using CoreCompatibilyzer.BannedApiData.Model;
using CoreCompatibilyzer.BannedApiData.Providers;

namespace CoreCompatibilyzer.BannedApiData.Storage
{
    /// <summary>
    /// A banned API storage helper that keeps and retrieves the banned API storage.
    /// </summary>
    public static partial class BannedApiStorage
    {
        private const string _bannedApiFileRelativePath = @"BannedApiData\Data\BannedApis.txt";
        private const string _bannedApiAssemblyResourceName = @"BannedApiData.Data.BannedApis.txt";

        private static readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        private static volatile IBannedApiStorage? _instance;

		public static IBannedApiStorage GetStorage(CancellationToken cancellation, IBannedApiDataProvider? customBannedApiDataProvider = null)
        {
			cancellation.ThrowIfCancellationRequested();

			if (_instance != null)
				return _instance;

			_initializationLock.Wait();

			try
			{
				if (_instance == null)
					_instance = GetStorageAsyncWithoutLocking(cancellation, customBannedApiDataProvider);

				return _instance;
			}
			finally
			{
				_initializationLock.Release();
			}
		}

		private static IBannedApiStorage GetStorageAsyncWithoutLocking(CancellationToken cancellation, IBannedApiDataProvider? customBannedApiDataProvider)
		{
			var bannedApiDataProvider = customBannedApiDataProvider ?? GetDefaultDataProvider();
			var bannedApis = bannedApiDataProvider.GetBannedApiData(cancellation);

			cancellation.ThrowIfCancellationRequested();

			return bannedApis == null
				? new DefaultBannedApiStorage()
				: new DefaultBannedApiStorage(bannedApis);
		}

		public static async Task<IBannedApiStorage> GetStorageAsync(CancellationToken cancellation, IBannedApiDataProvider? customBannedApiDataProvider = null)
        {
			cancellation.ThrowIfCancellationRequested();

            if (_instance != null)
                return _instance;

			await _initializationLock.WaitAsync(cancellation).ConfigureAwait(false);

			try
			{
				if (_instance == null)
					_instance = await GetStorageAsyncWithoutLockingAsync(cancellation, customBannedApiDataProvider).ConfigureAwait(false);

				return _instance;
			}
			finally
			{
				_initializationLock.Release();
			}		
		}

        private static async Task<IBannedApiStorage> GetStorageAsyncWithoutLockingAsync(CancellationToken cancellation, IBannedApiDataProvider? customBannedApiDataProvider)
        {
			var bannedApiDataProvider = customBannedApiDataProvider ?? GetDefaultDataProvider();

			var bannedApis = await bannedApiDataProvider.GetBannedApiDataAsync(cancellation).ConfigureAwait(false);
			cancellation.ThrowIfCancellationRequested();

			return bannedApis == null
				? new DefaultBannedApiStorage()
				: new DefaultBannedApiStorage(bannedApis);
		}

		private static IBannedApiDataProvider GetDefaultDataProvider()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, _bannedApiFileRelativePath);
            Assembly assembly = typeof(BannedApiStorage).Assembly;
            string assemblyName = assembly.GetName().Name;
            string fullResourceName = $"{assemblyName}.{_bannedApiAssemblyResourceName}";

            var providers = new IBannedApiDataProvider[]
            {
                new FileDataProvider(filePath),
                new AssemblyResourcesDataProvider(assembly, fullResourceName)
            };

            var defaultProvider = new DataProvidersCoalesceCombinator(providers);
            return defaultProvider;
        }
	}
}
