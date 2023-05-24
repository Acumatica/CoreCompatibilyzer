using System;
using System.Collections.Generic;

using CoreCompatibilyzer.Constants;
using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.ApiData.Model
{
	/// <summary>
	/// API data.
	/// </summary>
	public readonly struct Api : IEquatable<Api>, IComparable<Api>
	{
		public string DocID { get; }

		public ApiKind Kind { get; }

		public string FullName { get; }

		public ApiInfoType ApiInfoType { get; }

        public Api(string docIDWithOptionalObsoleteMarker, bool isWhiteList)
        {
			docIDWithOptionalObsoleteMarker = docIDWithOptionalObsoleteMarker.ThrowIfNullOrWhiteSpace(nameof(docIDWithOptionalObsoleteMarker)).Trim();

			if (docIDWithOptionalObsoleteMarker.Length < 2)
				throw InvalidInputStringFormatException(docIDWithOptionalObsoleteMarker);

			if (char.IsWhiteSpace(docIDWithOptionalObsoleteMarker[^2]))
			{
				if (char.ToUpper(docIDWithOptionalObsoleteMarker[^1]) != CommonConstants.ApiObsoletionMarker)
					throw InvalidInputStringFormatException(docIDWithOptionalObsoleteMarker);

				DocID = docIDWithOptionalObsoleteMarker.Remove(docIDWithOptionalObsoleteMarker.Length - 2);
				ApiInfoType = isWhiteList ? ApiInfoType.WhiteList : ApiInfoType.Obsolete;
			}
			else
			{
				DocID = docIDWithOptionalObsoleteMarker;
				ApiInfoType = isWhiteList ? ApiInfoType.WhiteList : ApiInfoType.NotPresentInNetCore;
			}
			
			Kind = DocID.GetApiKind();

			if (Kind == ApiKind.Undefined || DocID.Length < 2)
				throw InvalidInputStringFormatException(DocID);
			
			FullName = DocID.Substring(2);
        }

		private static ArgumentException InvalidInputStringFormatException(string docID) =>
			 new ArgumentException($"The input API DocID string \"{docID}\" has unknown format.\r\n" +
									"Please check the following link for a list of supported formats: " +
									"https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/#id-strings" + 
									$"\r\n\r\nCoreCompatibilyzer extends the DocID string format above with an indicator \"{CommonConstants.ApiObsoletionMarker}\"" + 
									" character separated by whitespace at the end of a DocID string",
									nameof(docID));

		public override bool Equals(object obj) => obj is Api api && Equals(api);

		public bool Equals(Api other) => 
			string.Equals(DocID, other.DocID) && ApiInfoType == other.ApiInfoType;

		public static bool operator ==(Api x, Api y) => x.Equals(y);

		public static bool operator !=(Api x, Api y) => !x.Equals(y);

		public override string ToString() => DocID;

		public override int GetHashCode() => DocID.GetHashCode();

		public int CompareTo(Api other) => DocID.CompareTo(other.DocID);
	}
}
