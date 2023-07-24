﻿using System;

namespace CoreCompatibilyzer.Constants
{
    public static class CommonConstants
    {
        public const char ApiObsoletionMarker 	= 'O';
		public const char NamespaceSeparator 	= '-';
		public const char NestedTypesSeparator = '+';

		public const string ApiDocIDWithObsoletionDiagnosticProperty = nameof(ApiDocIDWithObsoletionDiagnosticProperty);
    }
}
