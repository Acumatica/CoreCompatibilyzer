using System;

namespace CoreCompatibilyzer.Constants
{
    public static class CommonConstants
    {
		public static class Chars
		{
			public const char ApiObsoletionMarker  = 'O';
			public const char NamespaceSeparator   = '-';
			public const char NestedTypesSeparator = '+';
		}

		public static class Strings
		{
			public const string ApiObsoletionMarker  = "O";
			public const string NamespaceSeparator 	 = "-";
			public const string NestedTypesSeparator = "+";
		}

		public const string ClosestBannedApiProperty = nameof(ClosestBannedApiProperty);
		public const string ApiFoundInDbProperty	 = nameof(ApiFoundInDbProperty);
    }
}
