using System;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace CoreCompatibilyzer.BannedApiData
{
	public static class ApiUtils
	{
		/// <summary>
		/// An extension method that gets API kind from the Doc ID.
		/// </summary>
		/// <param name="docId">The docId to act on.</param>
		/// <returns>
		/// The API kind.
		/// </returns>
		public static ApiKind GetApiKind(this string docId)
		{
			if (string.IsNullOrWhiteSpace(docId) || docId!.Length <= 2 || docId[1] != ':')
				return ApiKind.Undefined;

			return docId[0] switch
			{
				'N' => ApiKind.Namespace,
				'T' => ApiKind.Type,
				'M' => ApiKind.Method,
				'F' => ApiKind.Field,
				'P' => ApiKind.Property,
				'E' => ApiKind.Event,
				_ => ApiKind.Undefined
			};
		}
	}
}
