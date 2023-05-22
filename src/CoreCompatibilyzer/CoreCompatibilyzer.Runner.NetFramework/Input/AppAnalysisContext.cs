﻿using System;

using CoreCompatibilyzer.DotNetRuntimeVersion;
using CoreCompatibilyzer.Runner.Analysis.CodeSources;
using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.Runner.Input
{
    internal class AppAnalysisContext
	{
		/// <summary>
		/// Gets the code source to validate.
		/// </summary>
		/// <value>
		/// The code source to validate.
		/// </value>
		public ICodeSource CodeSource { get; }

		/// <summary>
		/// Gets target .Net runtime version that will be used for the compatibility checks.
		/// </summary>
		/// <value>
		/// The target .Net runtime version that will be used for the compatibility checks.
		/// </value>
		public DotNetRuntime TargetRuntime { get; }

		/// <inheritdoc cref="CommandLineOptions.MSBuildPath"/>
		public string? MSBuildPath { get; }


		/// <inheritdoc cref="CommandLineOptions.DisableSuppressionMechanism"/>
		public bool DisableSuppressionMechanism { get; }


		public AppAnalysisContext(ICodeSource codeSource, DotNetRuntime targetRuntime, bool disableSuppressionMechanism, string? msBuildPath)
		{
			CodeSource 					= codeSource.ThrowIfNull(nameof(codeSource));
			TargetRuntime 				= targetRuntime;
			DisableSuppressionMechanism = disableSuppressionMechanism;
			MSBuildPath 				= msBuildPath.NullIfWhiteSpace();
		}
	}
}