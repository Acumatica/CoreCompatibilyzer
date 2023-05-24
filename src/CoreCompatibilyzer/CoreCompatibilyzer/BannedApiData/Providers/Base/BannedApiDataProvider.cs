using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using CoreCompatibilyzer.BannedApiData.Model;

namespace CoreCompatibilyzer.BannedApiData.Providers
{
    /// <summary>
    /// A banned API data provider base class to share some common logic.
    /// </summary>
    public abstract class BannedApiDataProvider : IBannedApiDataProvider
	{
		/// <inheritdoc/>
		public abstract bool IsDataAvailable { get; }

		/// <inheritdoc/>
		public abstract IEnumerable<Api>? GetBannedApiData(CancellationToken cancellation);

		/// <inheritdoc/>
		public abstract Task<IEnumerable<Api>?> GetBannedApiDataAsync(CancellationToken cancellation);

		protected IEnumerable<Api> ParseTextIntoBannedApis(string text, CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();

			int lineNumber = 1;
			using StringReader wholeTextReader = new StringReader(text);

			while (wholeTextReader.ReadLine() is string rawDocID)
			{
				cancellation.ThrowIfCancellationRequested();

				Api bannedApi = ParseBannedApiFromLine(rawDocID, lineNumber);
				yield return bannedApi;

				lineNumber++;
			}
		}

		protected IEnumerable<Api> ParseLinesIntoBannedApis(IEnumerable<string> rawLines, CancellationToken cancellation)
		{
			int lineNumber = 1;

			foreach (string rawDocID in rawLines)
			{
				cancellation.ThrowIfCancellationRequested();

				Api bannedApi = ParseBannedApiFromLine(rawDocID, lineNumber);
				yield return bannedApi;

				lineNumber++;
			}
		}

		protected IEnumerable<Api> ParseStreamIntoBannedApis(Stream resourceStream, CancellationToken cancellation)
		{
			int lineNumber = 1;
			using var reader = new StreamReader(resourceStream);

			while (reader.ReadLine() is string rawDocID)
			{
				cancellation.ThrowIfCancellationRequested();

				Api bannedApi = ParseBannedApiFromLine(rawDocID, lineNumber);
				yield return bannedApi;

				lineNumber++;
			}
		}

		protected virtual Api ParseBannedApiFromLine(string rawDocID, int lineNumber)
		{
			try
			{
				return new Api(rawDocID);
			}
			catch (Exception exception)
			{
				string errorMessage = GetParseErrorMessage(exception, lineNumber);
				throw new Errors.BannedApiReaderException(errorMessage, exception);
			}
		}

		protected abstract string GetParseErrorMessage(Exception originalException, int lineNumber);
	}
}
