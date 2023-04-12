using System;
using System.Collections.Generic;

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

        public BannedApi(string docID)
        {
			DocID = docID.ThrowIfNullOrWhiteSpace(nameof(docID)).Trim();
			Kind = DocID.GetApiKind();

			if (Kind == ApiKind.Undefined || DocID.Length < 2)
			{
				throw new ArgumentException($"The input API DocID string \"{docID}\" has unknown format.{Environment.NewLine}" +
											"Please check the following link for a list of supported formats: " +
											"https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/#id-strings",
											nameof(docID));
			}
			
			FullName = DocID.Substring(2);
        }

		public override bool Equals(object obj) => obj is BannedApi bannedApi && Equals(bannedApi);

		public bool Equals(BannedApi other) => string.Equals(DocID, other.DocID);

		public static bool operator ==(BannedApi x, BannedApi y) => x.Equals(y);

		public static bool operator !=(BannedApi x, BannedApi y) => !x.Equals(y);

		public override string ToString() => DocID;

		public override int GetHashCode() => DocID.GetHashCode();

		public int CompareTo(BannedApi other) => DocID.CompareTo(other.DocID);
	}
}
