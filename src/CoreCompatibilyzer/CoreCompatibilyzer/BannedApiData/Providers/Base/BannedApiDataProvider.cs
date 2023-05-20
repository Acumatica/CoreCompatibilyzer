using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CoreCompatibilyzer.BannedApiData
{
    /// <summary>
    /// A banned API data provider base class to share some common logic.
    /// </summary>
    public abstract class BannedApiDataProvider : IBannedApiDataProvider
	{
		/// <inheritdoc/>
		public abstract bool IsDataAvailable { get; }

		/// <inheritdoc/>
		public abstract IEnumerable<BannedApi>? GetBannedApiData(CancellationToken cancellation);

		/// <inheritdoc/>
		public abstract Task<IEnumerable<BannedApi>?> GetBannedApiDataAsync(CancellationToken cancellation);

		protected IEnumerable<BannedApi> ParseTextIntoBannedApis(string text, CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();

			int lineNumber = 1;
			using StringReader wholeTextReader = new StringReader(text);

			while (wholeTextReader.ReadLine() is string rawDocID)
			{
				cancellation.ThrowIfCancellationRequested();

				BannedApi bannedApi = ParseBannedApiFromLine(rawDocID, lineNumber);
				yield return bannedApi;

				lineNumber++;
			}
		}

		protected IEnumerable<BannedApi> ParseLinesIntoBannedApis(IEnumerable<string> rawLines, CancellationToken cancellation)
		{
			int lineNumber = 1;

			foreach (string rawDocID in rawLines)
			{
				cancellation.ThrowIfCancellationRequested();

				BannedApi bannedApi = ParseBannedApiFromLine(rawDocID, lineNumber);
				yield return bannedApi;

				lineNumber++;
			}
		}

		protected IEnumerable<BannedApi> ParseStreamIntoBannedApis(Stream resourceStream, CancellationToken cancellation)
		{
			int lineNumber = 1;
			using var reader = new StreamReader(resourceStream);

			while (reader.ReadLine() is string rawDocID)
			{
				cancellation.ThrowIfCancellationRequested();

				BannedApi bannedApi = ParseBannedApiFromLine(rawDocID, lineNumber);
				yield return bannedApi;

				lineNumber++;
			}
		}

		protected virtual BannedApi ParseBannedApiFromLine(string rawDocID, int lineNumber)
		{
			try
			{
				return new BannedApi(rawDocID);
			}
			catch (Exception exception)
			{
				string errorMessage = GetParseErrorMessage(exception, lineNumber);
				throw new BannedApiReaderException(errorMessage, exception);
			}
		}

		protected abstract string GetParseErrorMessage(Exception originalException, int lineNumber);
	}
}
