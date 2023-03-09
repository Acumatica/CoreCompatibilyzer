using System;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace CoreCompatibilyzer.Constants
{
    public static class CommonConstants
    {
        /// <summary>
        /// (Immutable) The compatibility analyzer diagnostics prefix.
        /// </summary>
        public const string DiagnosticsPrefix = "CoreCompat";


        public static class Types
        {
            public static readonly string TargetFrameworkAttribute = typeof(TargetFrameworkAttribute).FullName;
        }
    }
}
