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
		private const int NameOffset = 2;

		public string DocID { get; }

		public ApiKind Kind { get; }

		public ApiExtraInfo ExtraInfo { get; }

        public Api(string docIDWithOptionalObsoleteMarker)
        {
			docIDWithOptionalObsoleteMarker = docIDWithOptionalObsoleteMarker.ThrowIfNullOrWhiteSpace(nameof(docIDWithOptionalObsoleteMarker)).Trim();

			if (docIDWithOptionalObsoleteMarker.Length < NameOffset)
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

			if (Kind == ApiKind.Undefined || DocID.Length < NameOffset)
				throw InvalidInputStringFormatException(DocID);
        }

		public string GetDocIDWithOptionalObsoleteMarker() =>
			ExtraInfo == ApiExtraInfo.Obsolete
				? $"{DocID} {CommonConstants.ApiObsoletionMarker}"
				: DocID;

		public string GetMemberName()
		{
			switch (Kind)
			{
				case ApiKind.Type:
				case ApiKind.Field:
				case ApiKind.Property:
				case ApiKind.Event:
					return GetLastNameSegment();
				case ApiKind.Method:
					return GetMethodLastNameSegment();
				default:
					return string.Empty;
			}
		}

		public string GetTypeName()
		{
			switch (Kind)
			{
				case ApiKind.Type:
					return GetLastNameSegment();
				case ApiKind.Field:
				case ApiKind.Property:
				case ApiKind.Event:
					return GetSecondFromTheEndNameSegment();
				case ApiKind.Method:
					return GetMethodSecondFromTheEndNameSegment();
				default:
					return string.Empty;
			}
		}

		public string GetNamespace()
		{
			switch (Kind)
			{
				case ApiKind.Namespace:
					return GetLastNameSegment();
				case ApiKind.Type:
				case ApiKind.Field:
				case ApiKind.Property:
				case ApiKind.Event:
				case ApiKind.Method:
					string typeName = GetTypeName();
					int typeNameIndex = DocID.LastIndexOf(typeName);

					if (typeNameIndex <= 0 || DocID[typeNameIndex - 1] != '.')
						return string.Empty;

					return DocID[NameOffset..(typeNameIndex - 1)];
				default:
					return string.Empty;
			}
		}

		public string GetFullName() => DocID.Substring(NameOffset);

		private string GetMethodLastNameSegment()
		{
			if (DocID[^1] == ')')
				return GetLastNameSegment();

			int startBraceIndex = DocID.LastIndexOf('(');

			if (startBraceIndex <= 0)
				return GetLastNameSegment();

			int lastSegmentDotIndex = DocID.LastIndexOf('.', startBraceIndex - 1);
			int lastSegmentStart = lastSegmentDotIndex >= NameOffset
				? lastSegmentDotIndex + 1
				: NameOffset;

			string lastSegment = DocID[lastSegmentStart..startBraceIndex];
			return lastSegment;
		}

		private string GetLastNameSegment()
		{
			int lastDotIndex = DocID.LastIndexOf('.');

			if (lastDotIndex < 0)
				return GetFullName();

			string lastSegment = DocID.Substring(lastDotIndex + 1);
			return lastSegment;
		}

		private string GetMethodSecondFromTheEndNameSegment()
		{
			if (DocID[^1] == ')')
				return GetSecondFromTheEndNameSegment();

			int startBraceIndex = DocID.LastIndexOf('(');

			if (startBraceIndex <= 0)
				return GetSecondFromTheEndNameSegment();

			int lastSegmentDotIndex = DocID.LastIndexOf('.', startBraceIndex - 1);
			return GetSecondFromTheEndNameSegment(lastSegmentDotIndex);
		}

		private string GetSecondFromTheEndNameSegment()
		{
			int lastDotIndex = DocID.LastIndexOf('.');
			return GetSecondFromTheEndNameSegment(lastDotIndex);
		}

		private string GetSecondFromTheEndNameSegment(int lastSegmentDotIndex)
		{
			if (lastSegmentDotIndex <= 0)
				return string.Empty;

			int secondFromTheEndDotIndex = DocID.LastIndexOf('.', lastSegmentDotIndex - 1);
			int secondFromEndSegmentStart = secondFromTheEndDotIndex >= NameOffset
				? secondFromTheEndDotIndex + 1
				: NameOffset;

			string secondFromEndSegment = DocID[secondFromEndSegmentStart..lastSegmentDotIndex];
			return secondFromEndSegment;
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
