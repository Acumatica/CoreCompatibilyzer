using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

using CoreCompatibilyzer.Utils.Common;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

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

		#region Editorconfig experimental code parts that need to be integrated into analyzer later
		/// <summary>
		/// Get analyzer diagnostics enabled by editor config for a syntax tree.
		/// </summary>
		/// <param name="analyzerConfigOptionsProvider">The analyzer configuration options provider.</param>
		/// <param name="analyzedTree">The analyzed tree.</param>
		/// <param name="supportedDiagnostics">The supported diagnostics.</param>
		/// <returns>
		/// Analyzer diagnostics enabled by editor config for a syntax tree.
		/// </returns>
		private static ImmutableArray<DiagnosticDescriptor> GetDiagnosticsEnabledInEditorConfig(AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider,
																								SyntaxTree analyzedTree,
																								ImmutableArray<DiagnosticDescriptor> supportedDiagnostics)
		{
			if (supportedDiagnostics.IsDefaultOrEmpty)
				return supportedDiagnostics;

			if (analyzerConfigOptionsProvider == null || analyzedTree == null)
				return supportedDiagnostics.Where(IsEnabledByDefault).ToImmutableArray();

			var alreadyCheckedDiagnostics = new List<string>(capacity: supportedDiagnostics.Length);
			var enabledDiagnostics = ImmutableArray.CreateBuilder<DiagnosticDescriptor>(supportedDiagnostics.Length);
			bool alreadyReadOptions = false;
			AnalyzerConfigOptions? analyzerConfigOptions = null;

			foreach (DiagnosticDescriptor diagnosticDescriptor in supportedDiagnostics)
			{
				if (alreadyCheckedDiagnostics.Contains(diagnosticDescriptor.Id))
					continue;

				alreadyCheckedDiagnostics.Add(diagnosticDescriptor.Id);
				bool canConfigure = (!alreadyReadOptions || analyzerConfigOptions != null) && CanBeConfiguredByEditorConfig(diagnosticDescriptor);

				if (canConfigure)
				{
					analyzerConfigOptions = analyzerConfigOptionsProvider.GetOptions(analyzedTree);
					alreadyReadOptions = true;

					if (IsEnabled(diagnosticDescriptor, analyzerConfigOptions))
						enabledDiagnostics.Add(diagnosticDescriptor);
				}
				else
				{
					if (IsEnabledByDefault(diagnosticDescriptor))
						enabledDiagnostics.Add(diagnosticDescriptor);
				}
			}

			return enabledDiagnostics.ToImmutable();
		}

		private static bool IsEnabled(DiagnosticDescriptor diagnosticDescriptor, AnalyzerConfigOptions? analyzerConfigOptions)
		{
			if (analyzerConfigOptions == null)
				return IsEnabledByDefault(diagnosticDescriptor);

			string diagnosticEnabledFlag = diagnosticDescriptor.GetEnabledFlagFullName();

			if (!analyzerConfigOptions.TryGetValue(diagnosticEnabledFlag, out string? isEnabledStrValue) || isEnabledStrValue.IsNullOrWhiteSpace())
				return IsEnabledByDefault(diagnosticDescriptor);

			return bool.TryParse(isEnabledStrValue, out bool isEnabled)
				? isEnabled
				: IsEnabledByDefault(diagnosticDescriptor);
		}

		/// <summary>
		/// Gets a value indicating whether the diagnostic is enabled by default.
		/// </summary>
		/// <param name="diagnosticDescriptor">Information describing the diagnostic.</param>
		/// <returns>
		/// True if enabled by default, false if not.
		/// </returns>
		private static bool IsEnabledByDefault(DiagnosticDescriptor diagnosticDescriptor) => true;

		private static bool CanBeConfiguredByEditorConfig(DiagnosticDescriptor diagnosticDescriptor) => true;
		#endregion
	}
}