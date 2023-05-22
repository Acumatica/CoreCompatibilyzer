using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics.CodeAnalysis;

using CoreCompatibilyzer.Utils.Common;
using CoreCompatibilyzer.BannedApiData.Model;

namespace CoreCompatibilyzer.BannedApiData.Providers
{
	[SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035:Do not use APIs banned for analyzers", Justification = "Need to load banned API database")]
	public class FileDataProvider : BannedApiDataProvider
	{
		private readonly string _filePath;

		/// <inheritdoc/>
		public override bool IsDataAvailable => File.Exists(_filePath);

		public FileDataProvider(string filePath)
		{
			_filePath = filePath.ThrowIfNullOrWhiteSpace(nameof(filePath));
		}

		/// <inheritdoc/>
		public override async Task<IEnumerable<BannedApi>?> GetBannedApiDataAsync(CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();

			if (!IsDataAvailable)
				return null;

			string wholeText;

			using (var reader = new StreamReader(_filePath))
			{
				wholeText = await reader.ReadToEndAsync().WithCancellation(cancellation)
														 .ConfigureAwait(false);
			}

			cancellation.ThrowIfCancellationRequested();

			if (wholeText.IsNullOrWhiteSpace())
				return Enumerable.Empty<BannedApi>();

			var bannedApis = ParseTextIntoBannedApis(wholeText, cancellation);
			return bannedApis;
		}

		/// <inheritdoc/>
		public override IEnumerable<BannedApi>? GetBannedApiData(CancellationToken cancellation) =>
			GetBannedApiDataFromFile(cancellation);

		private IEnumerable<BannedApi>? GetBannedApiDataFromFile(CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();

			if (!IsDataAvailable)
				return null;
	
			var lines	   = File.ReadLines(_filePath);
			var bannedApis = ParseLinesIntoBannedApis(lines, cancellation);

			return bannedApis;
		}

		protected override string GetParseErrorMessage(Exception originalException, int lineNumber) =>
			$"An error happened during the reading of the line {lineNumber} from the file \"{_filePath}\"";
	}
}
