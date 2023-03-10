using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.Runner.Input
{
	internal class AnalysisContext
	{
		/// <summary>
		/// Gets a value indicating whether to apply the Acumatica specific validation rules to decide if project requries an XML documentation file.
		/// False by default.
		/// </summary>
		/// <value>
		/// True if apply Acumatica specific validation rules, false if not.
		/// </value>
		public bool ApplyAcumaticaSpecificValidationRules { get; }

		/// <summary>
		/// Gets a value indicating whether the analyser will react on the suppressed documentation generation settings in the project 
		/// and report the project as not having a documentation file specified. False by default.
		/// </summary>
		/// <value>
		/// True if report project with suppressed documentation generation, false if not.
		/// </value>
		public bool ReportDocGenerationSuppression { get; }

		/// <summary>
		/// If this flag is set to true then the tool won't consider a project if it contains only DACs marked with the PXHidden attribute
		/// as requiring documentation.
		/// </summary>
		public bool SkipPXHidden { get; }

		/// <summary>
		/// If this flag is set to true then the tool won't consider a project if it contains only DACs marked as internal API with the PX.Common.PXInternalUseOnlyAttribute
		/// as requiring documentation.
		/// </summary>
		public bool SkipInternalApi { get; }


		/// <summary>
		/// Optional explicitly specified path to MSBuild. Can be null. If null then MSBuild path is retrieved automatically.
		/// </summary>
		/// <value>
		/// The optional explicitly specified path to MSBuild.
		/// </value>
		public string? MSBuildPath { get; }

		public IReadOnlyList<ICodeSource> CodeSources { get; }

		public AnalysisContext(bool applyAcumaticaSpecificValidationRules, bool reportDocGenerationSuppression, bool skipPXHidden,
							   bool skipInternalApi, IEnumerable<ICodeSource>? codeSources, string? msBuildPath)
		{
			ApplyAcumaticaSpecificValidationRules = applyAcumaticaSpecificValidationRules;
			ReportDocGenerationSuppression = reportDocGenerationSuppression;
			SkipPXHidden = skipPXHidden;
			SkipInternalApi = skipInternalApi;
			CodeSources = codeSources?.ToImmutableArray() ?? ImmutableArray<ICodeSource>.Empty;
			MSBuildPath = msBuildPath.NullIfWhitespace();
		}
	}
}
