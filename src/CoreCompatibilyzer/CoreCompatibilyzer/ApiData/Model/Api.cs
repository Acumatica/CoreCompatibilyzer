using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using CoreCompatibilyzer.Constants;
using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.ApiData.Model
{
	/// <summary>
	/// API data.
	/// </summary>
	public class Api : IEquatable<Api>, IComparable<Api>
	{
		private const int NameOffset = 2;

		public string RawApiData { get; }

		public string DocID { get; }

		public string FullName { get; }

		public string Namespace { get; }

		public ImmutableArray<string> ContainingTypes { get; }

		public string TypeName { get; }

		public string FullTypeName { get; }

		public string MemberName { get; }

		public ApiKind Kind { get; }

		public ApiExtraInfo ExtraInfo { get; }

        public Api(string rawApiData)
        {
			RawApiData = rawApiData.ThrowIfNullOrWhiteSpace(nameof(rawApiData)).Trim();

			if (RawApiData.Length < NameOffset)
				throw InvalidInputStringFormatException(RawApiData);

			string apiDataWithoutObsoleteMarker;

			if (char.IsWhiteSpace(RawApiData[^2]))
			{
				if (char.ToUpper(RawApiData[^1]) != CommonConstants.ApiObsoletionMarker)
					throw InvalidInputStringFormatException(RawApiData);

				apiDataWithoutObsoleteMarker = RawApiData.Remove(RawApiData.Length - 2);
				ExtraInfo = ApiExtraInfo.Obsolete;
			}
			else
			{
				apiDataWithoutObsoleteMarker = RawApiData;
				ExtraInfo = ApiExtraInfo.None;
			}

			Kind = apiDataWithoutObsoleteMarker.GetApiKind();

			if (Kind == ApiKind.Undefined || apiDataWithoutObsoleteMarker.Length < NameOffset)
				throw InvalidInputStringFormatException(RawApiData);

			DocID	 = GetDocID(apiDataWithoutObsoleteMarker, Kind);
			FullName = DocID.Substring(NameOffset);

			string apiDataWithoutObsoleteMarkerAndPrefix	 = apiDataWithoutObsoleteMarker.Substring(NameOffset);
			(Namespace, string combinedTypeName, MemberName) = GetNameParts(apiDataWithoutObsoleteMarkerAndPrefix, Kind);
			(TypeName, ContainingTypes)						 = GetTypeParts(combinedTypeName);
			FullTypeName									 = $"{Namespace}.{combinedTypeName}";
		}

		private static string GetDocID(string apiDataWithoutObsoleteMarker, ApiKind apiKind)
		{
			if (apiKind == ApiKind.Namespace)
				return apiDataWithoutObsoleteMarker;

			var sb = new StringBuilder(apiDataWithoutObsoleteMarker)
							.Replace(CommonConstants.NamespaceSeparator, '.')
							.Replace(CommonConstants.NestedTypesSeparator, '.');
			return sb.ToString();
		}

		private static (string Namespace, string CombinedTypeName, string MemberName) GetNameParts(string apiDataWithoutObsoleteMarkerAndPrefix, ApiKind apiKind)
		{
			if (apiKind == ApiKind.Namespace)
				return (apiDataWithoutObsoleteMarkerAndPrefix, CombinedTypeName: string.Empty, MemberName: string.Empty);

			int namespaceSeparatorIndex = apiDataWithoutObsoleteMarkerAndPrefix.IndexOf(CommonConstants.NamespaceSeparator);
			string @namespace = namespaceSeparatorIndex > 0
				? apiDataWithoutObsoleteMarkerAndPrefix[..namespaceSeparatorIndex]
				: string.Empty;

			if (namespaceSeparatorIndex == apiDataWithoutObsoleteMarkerAndPrefix.Length - 1)
				return (@namespace, CombinedTypeName: string.Empty, MemberName: string.Empty);

			string typeAndMemberName = apiDataWithoutObsoleteMarkerAndPrefix[(namespaceSeparatorIndex + 1)..];
			var (combinedTypeName, memberName) = GetTypeAndMemberNameParts(typeAndMemberName, apiKind);
			return (@namespace, combinedTypeName, memberName);
		}

		private static (string CombinedTypeName, string MemberName) GetTypeAndMemberNameParts(string typeAndMemberName, ApiKind apiKind)
		{
			if (apiKind == ApiKind.Type)
				return (typeAndMemberName, MemberName: string.Empty);
			else if (apiKind != ApiKind.Method)
				return GetTypeAndMemberNamesForMemberApi(typeAndMemberName);

			int startBraceIndex = typeAndMemberName.LastIndexOf('(');

			if (startBraceIndex <= 0)
				return GetTypeAndMemberNamesForMemberApi(typeAndMemberName);

			string typeAndMemberNameWithoutBraces = typeAndMemberName[..startBraceIndex];
			string parameters 					  = typeAndMemberName[startBraceIndex..];
			var (combinedTypeName, memberName) 	  = GetTypeAndMemberNamesForMemberApi(typeAndMemberNameWithoutBraces);
			memberName							 += parameters;

			return (combinedTypeName, memberName);
		}

		private static (string CombinedTypeName, string MemberName) GetTypeAndMemberNamesForMemberApi(string typeAndMemberNameWithoutBraces)
		{
			int lastDotIndex = typeAndMemberNameWithoutBraces.LastIndexOf('.');

			if (lastDotIndex < 0)
				return (CombinedTypeName: string.Empty, MemberName: typeAndMemberNameWithoutBraces);

			string combinedTypeName = typeAndMemberNameWithoutBraces[..lastDotIndex];
			string memberName = lastDotIndex < (typeAndMemberNameWithoutBraces.Length - 1)
				? typeAndMemberNameWithoutBraces[(lastDotIndex + 1)..]
				: string.Empty;
			
			return (combinedTypeName, memberName);
		}

		private static (string TypeName, ImmutableArray<string> ContainingTypes) GetTypeParts(string combinedTypeName)
		{
			if (combinedTypeName.Length == 0)
				return (TypeName: string.Empty, ContainingTypes: ImmutableArray<string>.Empty);

			int typesSeparatorIndex = combinedTypeName.IndexOf(CommonConstants.NestedTypesSeparator);

			if (typesSeparatorIndex < 0)
				return (TypeName: combinedTypeName, ContainingTypes: ImmutableArray<string>.Empty);

			string[] types 		= combinedTypeName.Split(new[] { CommonConstants.NestedTypesSeparator }, StringSplitOptions.None);
			string typeName 	= types[^1];
			var containingTypes = types.Take(types.Length - 1).ToImmutableArray();
			
			return (typeName, containingTypes);
		}

		private static ArgumentException InvalidInputStringFormatException(string rawApiData) =>
			 new ArgumentException($"The input API data string \"{rawApiData}\" has unknown format.\r\n" +
									"Please check the following link for a list of supported formats: " +
									"https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/#id-strings" + 
									$"\r\n\r\nCoreCompatibilyzer extends the DocID string format above with an indicator \"{CommonConstants.ApiObsoletionMarker}\"" + 
									" character separated by whitespace at the end of a DocID string.\r\n" +
									$"It also uses the \"{CommonConstants.NamespaceSeparator}\" to separate the namespace part of the API name from the rest of its name and " +
									$"\"{CommonConstants.NestedTypesSeparator}\" to separate names of nested types",
									nameof(rawApiData));

		public override bool Equals(object obj) => obj is Api api && Equals(api);

		public bool Equals(Api other) => 
			string.Equals(DocID, other.DocID) && ExtraInfo == other.ExtraInfo;

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
