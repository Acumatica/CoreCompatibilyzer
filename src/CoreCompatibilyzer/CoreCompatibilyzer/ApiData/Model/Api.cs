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

		public ApiExtraInfo ExtraInfo { get; }

        public Api(string docIDWithOptionalObsoleteMarker)
        {
			docIDWithOptionalObsoleteMarker = docIDWithOptionalObsoleteMarker.ThrowIfNullOrWhiteSpace(nameof(docIDWithOptionalObsoleteMarker)).Trim();

			if (docIDWithOptionalObsoleteMarker.Length < 2)
				throw InvalidInputStringFormatException(docIDWithOptionalObsoleteMarker);

			if (char.IsWhiteSpace(docIDWithOptionalObsoleteMarker[^2]))
			{
				if (char.ToUpper(docIDWithOptionalObsoleteMarker[^1]) != CommonConstants.ApiObsoletionMarker)
					throw InvalidInputStringFormatException(docIDWithOptionalObsoleteMarker);

				DocID = docIDWithOptionalObsoleteMarker.Remove(docIDWithOptionalObsoleteMarker.Length - 2);
				ExtraInfo = ApiExtraInfo.Obsolete;
			}
			else
			{
				DocID = docIDWithOptionalObsoleteMarker;
				ExtraInfo = ApiExtraInfo.None;
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
			string.Equals(DocID, other.DocID) && ExtraInfo == other.ExtraInfo;

		public static bool operator ==(Api x, Api y) => x.Equals(y);

		public static bool operator !=(Api x, Api y) => !x.Equals(y);

		public override string ToString() => 
			ExtraInfo == ApiExtraInfo.Obsolete
				? $"{DocID} {ApiExtraInfo.Obsolete}"
				: DocID;

		public override int GetHashCode()
		{
			int hash = 17;

			unchecked
			{
				hash = 23 * hash + DocID.GetHashCode();
				hash = 23 * hash + (int)ExtraInfo;
			}

			return hash;
		}

		public int CompareTo(Api other)
		{
			var docIdCompareResult = DocID.CompareTo(other.DocID);

			if (docIdCompareResult == 0 || ExtraInfo == other.ExtraInfo)
				return 0;
			else if (ExtraInfo == ApiExtraInfo.None)
				return -1;
			else
				return 1;
		}
	}
}
