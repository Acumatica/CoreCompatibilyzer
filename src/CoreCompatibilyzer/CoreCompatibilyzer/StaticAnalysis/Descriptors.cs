using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using CoreCompatibilyzer.Utils.Common;
using CoreCompatibilyzer.Utils.Resources;

using static CoreCompatibilyzer.StaticAnalysis.Constants;

namespace CoreCompatibilyzer.StaticAnalysis
{
    public enum Category
	{
		Default,
		DotNetCoreCompatibility
	}

	public static class Descriptors
	{
		private const string DocumentationLinkPrefix = @"TODO";
		private const string DocumentatonFileExtension = "md";

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

		public static DiagnosticDescriptor CoreCompat1001_NotCompatibleWithCore { get; } =
			Rule($"{DiagnosticsPrefix}1001", nameof(Diagnostics.CoreCompat1001Title).GetLocalizedDiagnosticName(), Category.DotNetCoreCompatibility,
				DiagnosticSeverity.Error, DiagnosticsShortName.CoreCompat1001, 
				messageFormat: nameof(Diagnostics.CoreCompat1001MessageFormat).GetLocalizedDiagnosticName(),
				description: nameof(Diagnostics.CoreCompat1001Description).GetLocalizedDiagnosticName());
	}
}
