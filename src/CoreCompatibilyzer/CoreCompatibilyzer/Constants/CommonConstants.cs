using System;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace CoreCompatibilyzer.Constants
{
    public static class CommonConstants
    {
        public const char ApiObsoletionMarker = 'O';

        public static class Types
        {
            public static readonly string TargetFrameworkAttribute = typeof(TargetFrameworkAttribute).FullName;
        }
    }
}
