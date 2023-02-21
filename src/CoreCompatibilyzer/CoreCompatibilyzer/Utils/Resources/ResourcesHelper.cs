using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using CoreCompatibilyzer.Utils.Common;

using Microsoft.CodeAnalysis;

namespace CoreCompatibilyzer.Utils.Resources
{
    public static class ResourcesHelper
    {
        public static LocalizableString GetLocalizedDiagnosticName(this string diagnosticResourceName)
        {
            return new LocalizableResourceString(diagnosticResourceName, Diagnostics.ResourceManager, typeof(Diagnostics));
        }

        public static LocalizableString GetLocalized<TResource>(this string resourceName, ResourceManager resourceManager)
        {
            resourceManager.ThrowIfNull(nameof(resourceManager));
            return new LocalizableResourceString(resourceName, resourceManager, typeof(TResource));
        }
    }
}
