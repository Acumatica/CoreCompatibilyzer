﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;

using CoreCompatibilyzer.Utils.Common;
using System.Threading;
using System.IO;

namespace CoreCompatibilyzer.BannedApiData
{
	public class AssemblyResourcesDataProvider : BannedApiDataProvider
	{
		private readonly Assembly _assembly;
		private readonly string _bannedApiResourceName;
		private bool? _isDataAvailable;

		/// <inheritdoc/>
		public override bool IsDataAvailable
		{
			get 
			{
				if (_isDataAvailable.HasValue)
					return _isDataAvailable.Value;

				var resourceInfo = _assembly.GetManifestResourceInfo(_bannedApiResourceName);
				_isDataAvailable = resourceInfo != null;

				return _isDataAvailable.Value;
			}
		}

		public AssemblyResourcesDataProvider(Assembly assembly, string bannedApiResourceName)
        {
			_assembly = assembly.ThrowIfNull(nameof(assembly));
			_bannedApiResourceName = bannedApiResourceName.ThrowIfNullOrWhiteSpace(nameof(bannedApiResourceName));
		}

		/// <inheritdoc/>
		public override async Task<IEnumerable<BannedApi>?> GetBannedApiDataAsync(CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();

			if (!IsDataAvailable)
				return null;

			string wholeText;

			using (var resourceStream = _assembly.GetManifestResourceStream(_bannedApiResourceName))
			{

				if (resourceStream == null)
					throw new BannedApiReaderException($"Can't find the source text with Resource ID \"{_bannedApiResourceName}\".");

				using (var reader = new StreamReader(resourceStream))
				{
					wholeText = await reader.ReadToEndAsync().WithCancellation(cancellation)
															 .ConfigureAwait(false);
				}
			}

			cancellation.ThrowIfCancellationRequested();

			if (wholeText.IsNullOrWhiteSpace())
				return Enumerable.Empty<BannedApi>();

			var bannedApis = ParseTextIntoBannedApis(wholeText, cancellation);
			return bannedApis;
		}

		/// <inheritdoc/>
		public override IEnumerable<BannedApi>? GetBannedApiData(CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();

			if (!IsDataAvailable)
				return null;

			using var resourceStream = _assembly.GetManifestResourceStream(_bannedApiResourceName);

			if (resourceStream == null)
				throw new BannedApiReaderException($"Can't find the source text with Resource ID \"{_bannedApiResourceName}\".");

			return ParseStreamIntoBannedApis(resourceStream, cancellation);
		}

		protected override string GetParseErrorMessage(Exception originalException, int lineNumber)
		{
			var assemblyName = _assembly.GetName().Name;
			return $"An error happened during the reading of the line {lineNumber} from the" + 
				   $" resource \"{_bannedApiResourceName}\" of the assembly \"{assemblyName}\"";
		}
	}
}