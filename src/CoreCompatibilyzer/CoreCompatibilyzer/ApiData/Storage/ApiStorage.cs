﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using CoreCompatibilyzer.ApiData.Providers;
using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.ApiData.Storage
{
    /// <summary>
    /// An API storage helper that keeps and retrieves the API storage.
    /// </summary>
    public partial class ApiStorage
    {
        private const string _bannedApiFileRelativePath = @"ApiData\Data\BannedApis.txt";
        private const string _bannedApiAssemblyResourceName = @"ApiData.Data.BannedApis.txt";

		private const string _whiteListFileRelativePath = @"ApiData\Data\WhiteList.txt";
		private const string _whiteListAssemblyResourceName = @"ApiData.Data.WhiteList.txt";

		private readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        private volatile IApiStorage? _instance;

		private readonly string? _dataFileRelativePath;
		private readonly string? _dataAssemblyResourceName;

		public static ApiStorage BannedApi { get; } = new ApiStorage(_bannedApiFileRelativePath, _bannedApiAssemblyResourceName);

		public static ApiStorage WhiteList { get; } = new ApiStorage(_whiteListFileRelativePath, _whiteListAssemblyResourceName);

		public ApiStorage(string? dataFileRelativePath, string? dataAssemblyResourceName)
		{
			_dataFileRelativePath = dataFileRelativePath.NullIfWhiteSpace();
			_dataAssemblyResourceName = dataAssemblyResourceName.NullIfWhiteSpace();
		}

		public IApiStorage GetStorage(CancellationToken cancellation, IApiDataProvider? customBannedApiDataProvider = null)
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

		private IApiStorage GetStorageAsyncWithoutLocking(CancellationToken cancellation, IApiDataProvider? customBannedApiDataProvider)
		{
			var bannedApiDataProvider = customBannedApiDataProvider ?? GetDefaultDataProvider();
			var bannedApis = bannedApiDataProvider.GetApiData(cancellation);

			cancellation.ThrowIfCancellationRequested();

			return bannedApis == null
				? new DefaultApiStorage()
				: new DefaultApiStorage(bannedApis);
		}

		public async Task<IApiStorage> GetStorageAsync(CancellationToken cancellation, IApiDataProvider? customBannedApiDataProvider = null)
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

        private async Task<IApiStorage> GetStorageAsyncWithoutLockingAsync(CancellationToken cancellation, IApiDataProvider? customBannedApiDataProvider)
        {
			var bannedApiDataProvider = customBannedApiDataProvider ?? GetDefaultDataProvider();

			var bannedApis = await bannedApiDataProvider.GetApiDataAsync(cancellation).ConfigureAwait(false);
			cancellation.ThrowIfCancellationRequested();

			return bannedApis == null
				? new DefaultApiStorage()
				: new DefaultApiStorage(bannedApis);
		}

		private IApiDataProvider GetDefaultDataProvider()
        {
			if (_dataFileRelativePath == null && _dataAssemblyResourceName == null)
				return new EmptyProvider(considerDataAvailable: false);

			Assembly assembly 		 = typeof(ApiStorage).Assembly;
			var fileDataProvider 	 = MakeFileDataProvider(assembly);
			var assemblyDataProvider = MakeAssemblyDataProvider(assembly);

			if (fileDataProvider == null)
				return assemblyDataProvider!;
			else if (assemblyDataProvider == null)
				return fileDataProvider;

			var apiDataProviders = new IApiDataProvider[] { fileDataProvider, assemblyDataProvider };
			var defaultProvider = new DataProvidersCoalesceCombinator(apiDataProviders);
			return defaultProvider;
        }

		private FileDataProvider? MakeFileDataProvider(Assembly currentAssembly)
		{
			if (_dataFileRelativePath == null || currentAssembly.Location.IsNullOrWhiteSpace())
				return null;

			string folderWithExtension = Path.GetDirectoryName(currentAssembly.Location);
			string filePath = Path.Combine(folderWithExtension, _dataFileRelativePath);

			return new FileDataProvider(filePath);
		}

		private AssemblyResourcesDataProvider? MakeAssemblyDataProvider(Assembly currentAssembly)
		{
			if (_dataAssemblyResourceName == null)
				return null;

			string assemblyName = currentAssembly.GetName().Name;
			string fullResourceName = $"{assemblyName}.{_dataAssemblyResourceName}";

			return new AssemblyResourcesDataProvider(currentAssembly, fullResourceName);
		}
	}
}
