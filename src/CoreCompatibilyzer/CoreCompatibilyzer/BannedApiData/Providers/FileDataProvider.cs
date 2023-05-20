using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.BannedApiData
{
	public class FileDataProvider : IBannedApiDataProvider
	{
		private readonly string _filePath;

		public bool IsDataAvailable => File.Exists(_filePath);

		public FileDataProvider(string filePath)
		{
			_filePath = filePath.ThrowIfNullOrWhiteSpace(nameof(filePath));
		}

		/// <summary>
		/// Gets the banned API data asynchronously from the provider or <see langword="null"/> if the provider's banned API data is not available. <br/>
		/// On the latter case the <see cref="IsDataAvailable"/> flag value is <see langword="false"/>.
		/// </summary>
		/// <param name="cancellation">A token that allows processing to be cancelled.</param>
		/// <returns>
		/// The task with banned API data.
		/// </returns>
		public async Task<IEnumerable<BannedApi>?> GetBannedApiDataAsync(CancellationToken cancellation)
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

			if (wholeText.IsNullOrWhiteSpace())
				return Enumerable.Empty<BannedApi>();

			return GetBannedApiDataFromMemoryText(wholeText, cancellation);
		}

		private IEnumerable<BannedApi> GetBannedApiDataFromMemoryText(string text, CancellationToken cancellation) 
		{
			cancellation.ThrowIfCancellationRequested();

			int lineNumber = 1;
			using StringReader wholeTextReader = new StringReader(text);

			while (wholeTextReader.ReadLine() is string rawDocID)
			{
				cancellation.ThrowIfCancellationRequested();

				BannedApi bannedApi = ReadLineFromFile(rawDocID, lineNumber);
				yield return bannedApi;

				lineNumber++;
			}
		}

		public IEnumerable<BannedApi>? GetBannedApiData(CancellationToken cancellation) =>
			GetBannedApiDataFromFile(cancellation);

		private IEnumerable<BannedApi>? GetBannedApiDataFromFile(CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();

			if (!IsDataAvailable)
				return null;
	
			var lines	   = File.ReadLines(_filePath);
			var bannedApis = ParseLinesIntoBannedApis(lines);

			return bannedApis;

			//-----------------------------------------Local Function------------------------------------
			IEnumerable<BannedApi> ParseLinesIntoBannedApis(IEnumerable<string> rawLines)
			{
				int lineNumber = 1;

				foreach (string rawDocID in rawLines)
				{
					cancellation.ThrowIfCancellationRequested();

					BannedApi bannedApi = ReadLineFromFile(rawDocID, lineNumber);
					yield return bannedApi;

					lineNumber++;
				}
			}
		}

		private BannedApi ReadLineFromFile(string rawDocID, int lineNumber) 
		{
			try
			{
				return new BannedApi(rawDocID);
			}
			catch (Exception exception)
			{
				throw new BannedApiReaderException($"An error happened during the reading of the line {lineNumber} from the file \"{_filePath}\"", exception);
			}
		}
	}
}
