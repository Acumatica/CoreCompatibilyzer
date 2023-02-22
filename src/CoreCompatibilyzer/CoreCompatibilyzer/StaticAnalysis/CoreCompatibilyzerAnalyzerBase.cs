using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;


namespace CoreCompatibilyzer.StaticAnalysis.NotCompatibleWorkspaces
{
    public abstract class CoreCompatibilyzerAnalyzerBase : DiagnosticAnalyzer
	{
		/// <summary>
		/// Get analyzer diagnostics enabled by editor config for a syntax tree.
		/// </summary>
		/// <param name="analyzerConfigOptionsProvider">The analyzer configuration options provider.</param>
		/// <param name="analyzedTree">The analyzed tree.</param>
		/// <returns>
		/// Analyzer diagnostics enabled by editor config for a syntax tree.
		/// </returns>
		protected ImmutableArray<DiagnosticDescriptor> GetDiagnosticsEnabledInEditorConfig(AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider, 
																						   SyntaxTree analyzedTree)
		{
			var supportedDiagnostics = SupportedDiagnostics;

			if (supportedDiagnostics.IsDefaultOrEmpty)
				return supportedDiagnostics;

			if (analyzerConfigOptionsProvider == null || analyzedTree == null)
				return supportedDiagnostics.Where(IsEnabledByDefault).ToImmutableArray();

			var alreadyCheckedDiagnostics = new HashSet<string>();
			var enabledDiagnostics = ImmutableArray.CreateBuilder<DiagnosticDescriptor>(supportedDiagnostics.Length);
			bool alreadyReadOptions = false;
			AnalyzerConfigOptions? analyzerConfigOptions = null;

			foreach (DiagnosticDescriptor diagnosticDescriptor in supportedDiagnostics)
			{
				if (!alreadyCheckedDiagnostics.Add(diagnosticDescriptor.Id))
					continue;

				bool canConfigure = (!alreadyReadOptions || analyzerConfigOptions != null) && CanBeConfiguredByEditorConfig(diagnosticDescriptor);

				if (canConfigure)
				{
					analyzerConfigOptions = analyzerConfigOptionsProvider.GetOptions(analyzedTree);
					alreadyReadOptions = true;



					string diagnosticEnabledFlag = diagnosticDescriptor.GetEnabledFlagFullName();
					config.TryGetValue("dotnet_diagnostic.MyRules0001.use_multiple_namespaces_in_a_file", out var configValue);
				}
				else
				{
					if (IsEnabledByDefault(diagnosticDescriptor))
						enabledDiagnostics.Add(diagnosticDescriptor);
				}
			}

			return enabledDiagnostics.ToImmutable();
		}

		private bool IsEnabledInEditorConfig(DiagnosticDescriptor diagnosticDescriptor, AnalyzerConfigOptions? analyzerConfigOptions)
		{

		}

		/// <summary>
		/// Gets a value indicating whether the diagnostic is enabled by default.
		/// </summary>
		/// <param name="diagnosticDescriptor">Information describing the diagnostic.</param>
		/// <returns>
		/// True if enabled by default, false if not.
		/// </returns>
		protected virtual bool IsEnabledByDefault(DiagnosticDescriptor diagnosticDescriptor) => true;

		protected virtual bool CanBeConfiguredByEditorConfig(DiagnosticDescriptor diagnosticDescriptor) => true;
	}
}
