using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;

using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.StaticAnalysis
{
	public static class EditorConfig
	{
		private const string DotNetDiagnosticOptionsPrefix = "dotnet_diagnostic";
		private const string Enabled = "enabled";

		public static string GetEnabledFlagFullName(this DiagnosticDescriptor diagnosticDescriptor) =>
			diagnosticDescriptor.GetDiagnosticOptionFullName(Enabled);

		public static string GetDiagnosticOptionFullName(this DiagnosticDescriptor diagnosticDescriptor, string optionName)
		{
			diagnosticDescriptor.ThrowIfNull(nameof(diagnosticDescriptor));
			return $"{DotNetDiagnosticOptionsPrefix}.{diagnosticDescriptor.Id}.{optionName.ThrowIfNullOrWhiteSpace(nameof(optionName))}";
		}
	}
}
