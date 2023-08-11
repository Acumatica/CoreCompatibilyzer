using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;

using CoreCompatibilyzer.Utils.Common;
using CoreCompatibilyzer.Utils.Resources;

namespace CoreCompatibilyzer.StaticAnalysis
{
    public enum Category
	{
		Default,
		DotNetCoreCompatibility
	}

	public static class Descriptors
	{
		private const string DocumentationLinkPrefix = @"https://github.com/Acumatica/CoreCompatibilyzer/blob/main/docs/diagnostics/";
		private const string DocumentatonFileExtension = "md";

		/// <summary>
		/// (Immutable) The compatibility analyzer diagnostics prefix.
		/// </summary>
		private const string DiagnosticsPrefix = "CoreCompat";

		private static string MakeDocumentationLink(string diagnosticID) =>
			$"{DocumentationLinkPrefix}/{diagnosticID}.{DocumentatonFileExtension}";

		private static DiagnosticDescriptor Rule(string id, LocalizableString title, Category category, DiagnosticSeverity defaultSeverity,
												 string diagnosticShortName, LocalizableString? messageFormat = null, LocalizableString? description = null,
												 string? diagnosticDefaultJustification = null)
		{
			bool isEnabledByDefault = true;
			messageFormat = messageFormat ?? title;
			string diagnosticLink = MakeDocumentationLink(id);
			string[] customTags = diagnosticDefaultJustification.IsNullOrWhiteSpace()
				? new[] { diagnosticShortName }
				: new[] { diagnosticShortName, diagnosticDefaultJustification };

			return new DiagnosticDescriptor(id, title, messageFormat, category.ToString(), defaultSeverity,
											isEnabledByDefault, description, diagnosticLink, customTags);
		}

		public static DiagnosticDescriptor CoreCompat1001_ApiNotPresentInDotNetCore { get; } =
			Rule($"{DiagnosticsPrefix}1001", nameof(Diagnostics.CoreCompat1001Title).GetLocalized(), Category.DotNetCoreCompatibility,
				DiagnosticSeverity.Error, DiagnosticsShortName.CoreCompat1001, 
				description: nameof(Diagnostics.CoreCompat1001Description).GetLocalized());

		public static DiagnosticDescriptor CoreCompat1002_ApiObsoleteInDotNetCore { get; } =
			Rule($"{DiagnosticsPrefix}1002", nameof(Diagnostics.CoreCompat1002Title).GetLocalized(), Category.DotNetCoreCompatibility,
				DiagnosticSeverity.Error, DiagnosticsShortName.CoreCompat1002,
				description: nameof(Diagnostics.CoreCompat1002Description).GetLocalized());
	}
}
