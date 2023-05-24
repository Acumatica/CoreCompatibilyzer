using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using CoreCompatibilyzer.Utils.Common;
using CoreCompatibilyzer.ApiData.Errors;
using CoreCompatibilyzer.ApiData.Model;

namespace CoreCompatibilyzer.ApiData.Providers
{
	public class AssemblyResourcesDataProvider : ApiDataProvider
	{
		private readonly Assembly _assembly;
		private readonly string _apiResourceName;
		private bool? _isDataAvailable;

		/// <inheritdoc/>
		public override bool IsDataAvailable
		{
			get 
			{
				if (_isDataAvailable.HasValue)
					return _isDataAvailable.Value;

				var resourceInfo = _assembly.GetManifestResourceInfo(_apiResourceName);
				_isDataAvailable = resourceInfo != null;

				return _isDataAvailable.Value;
			}
		}

		public AssemblyResourcesDataProvider(Assembly assembly, string apiResourceName)
        {
			_assembly = assembly.ThrowIfNull(nameof(assembly));
			_apiResourceName = apiResourceName.ThrowIfNullOrWhiteSpace(nameof(apiResourceName));
		}

		/// <inheritdoc/>
		public override async Task<IEnumerable<Api>?> GetApiDataAsync(CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();

			if (!IsDataAvailable)
				return null;

			string wholeText;

			using (var resourceStream = _assembly.GetManifestResourceStream(_apiResourceName))
			{

				if (resourceStream == null)
					throw new ApiReaderException($"Can't find the source text with Resource ID \"{_apiResourceName}\".");

				using (var reader = new StreamReader(resourceStream))
				{
					wholeText = await reader.ReadToEndAsync().WithCancellation(cancellation)
															 .ConfigureAwait(false);
				}
			}

			cancellation.ThrowIfCancellationRequested();

			if (wholeText.IsNullOrWhiteSpace())
				return Enumerable.Empty<Api>();

			var bannedApis = ParseTextIntoApis(wholeText, cancellation);
			return bannedApis;
		}

		/// <inheritdoc/>
		public override IEnumerable<Api>? GetApiData(CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();

			if (!IsDataAvailable)
				return null;

			using (var resourceStream = _assembly.GetManifestResourceStream(_apiResourceName))
			{

				if (resourceStream == null)
					throw new ApiReaderException($"Can't find the source text with Resource ID \"{_apiResourceName}\".");

				return ParseStreamIntoApis(resourceStream, cancellation).ToList();
			}
		}

		protected override string GetParseErrorMessage(Exception originalException, int lineNumber)
		{
			var assemblyName = _assembly.GetName().Name;
			return $"An error happened during the reading of the line {lineNumber} from the" + 
				   $" resource \"{_apiResourceName}\" of the assembly \"{assemblyName}\"";
		}
	}
}
