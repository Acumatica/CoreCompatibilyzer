using System;
using System.Collections.Generic;
using System.Security.Cryptography;

using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.BannedApiData
{
	/// <summary>
	/// A banned API data.
	/// </summary>
	public readonly struct BannedApi : IEquatable<BannedApi>, IComparable<BannedApi>
	{
		public string DocID { get; }

		public ApiKind Kind { get; }

		public string FullName { get; }

		public BannedApiType BannedApiType { get; }

        public BannedApi(string docIDWithOptionalObsoleteMarker)
        {
			docIDWithOptionalObsoleteMarker = docIDWithOptionalObsoleteMarker.ThrowIfNullOrWhiteSpace(nameof(docIDWithOptionalObsoleteMarker)).Trim();

			if (docIDWithOptionalObsoleteMarker.Length < 2)
				throw InvalidInputStringFormatException(docIDWithOptionalObsoleteMarker);

			if (char.IsWhiteSpace(docIDWithOptionalObsoleteMarker[^2]))
			{
				if (char.ToUpper(docIDWithOptionalObsoleteMarker[^1]) != 'O')
					throw InvalidInputStringFormatException(docIDWithOptionalObsoleteMarker);

				DocID = docIDWithOptionalObsoleteMarker.Remove(docIDWithOptionalObsoleteMarker.Length - 2);
				BannedApiType = BannedApiType.Obsolete;
			}
			else
			{
				DocID = docIDWithOptionalObsoleteMarker;
				BannedApiType = BannedApiType.NotPresentInNetCore;
			}
			
			Kind = DocID.GetApiKind();

			if (Kind == ApiKind.Undefined || DocID.Length < 2)
				throw InvalidInputStringFormatException(DocID);
			
			FullName = DocID.Substring(2);
        }

		private static ArgumentException InvalidInputStringFormatException(string docID) =>
			 new ArgumentException($"The input API DocID string \"{docID}\" has unknown format.{Environment.NewLine}" +
									"Please check the following link for a list of supported formats: " +
									"https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/#id-strings" + 
									Environment.NewLine + Environment.NewLine +
									"CoreCompatibilyzer extends the DocID string format above with an indicator \"O\" character separated by whitespace at the end of a DocID string",
									nameof(docID));

		public override bool Equals(object obj) => obj is BannedApi bannedApi && Equals(bannedApi);

		public bool Equals(BannedApi other) => 
			string.Equals(DocID, other.DocID) && BannedApiType == other.BannedApiType;

		public static bool operator ==(BannedApi x, BannedApi y) => x.Equals(y);

		public static bool operator !=(BannedApi x, BannedApi y) => !x.Equals(y);

		public override string ToString() => DocID;

		public override int GetHashCode() => DocID.GetHashCode();

		public int CompareTo(BannedApi other) => DocID.CompareTo(other.DocID);
	}
}
