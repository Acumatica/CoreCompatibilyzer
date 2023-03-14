using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Runtime.Versioning;

using CoreCompatibilyzer.Utils.Common;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;


namespace CoreCompatibilyzer.StaticAnalysis.NotCompatibleWorkspaces
{
    public abstract class CoreCompatibilyzerAnalyzerBase : DiagnosticAnalyzer
	{
		public sealed override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze); // We want to analyze for compatibility even generated code
			context.EnableConcurrentExecution();

			//context.RegisterCompilationStartAction(c => c.Compilation.)
			
			RegisterAnalysisActions(context);
		}

		protected abstract void RegisterAnalysisActions(AnalysisContext context);

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

		private bool IsEnabled(DiagnosticDescriptor diagnosticDescriptor, AnalyzerConfigOptions? analyzerConfigOptions)
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
		protected virtual bool IsEnabledByDefault(DiagnosticDescriptor diagnosticDescriptor) => true;

		protected virtual bool CanBeConfiguredByEditorConfig(DiagnosticDescriptor diagnosticDescriptor) => true;
	}
}
