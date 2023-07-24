﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using CoreCompatibilyzer.DotNetRuntimeVersion;
using CoreCompatibilyzer.Runner.Analysis.CodeSources;
using CoreCompatibilyzer.Runner.Constants;
using CoreCompatibilyzer.Runner.ReportFormat;
using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.Runner.Input
{
    internal class AnalysisContextBuilder
	{
		public AppAnalysisContext CreateContext(CommandLineOptions commandLineOptions)
		{
			commandLineOptions.ThrowIfNull(nameof(commandLineOptions));
			var codeSource = ReadCodeSource(commandLineOptions.CodeSource);

			if (codeSource == null)
				throw new ArgumentException("Code source is not specified");

			FormatMode formatMode = commandLineOptions.IncludeApiUsages 
				? FormatMode.UsedAPIsWithUsages 
				: FormatMode.UsedAPIsOnly;

			GroupingMode groupingMode = ReadGroupingMode(commandLineOptions.ReportGrouping);
			var input = new AppAnalysisContext(codeSource, targetRuntime: DotNetRuntime.DotNetCore22, commandLineOptions.DisableSuppressionMechanism,
											   commandLineOptions.MSBuildPath, formatMode, groupingMode, commandLineOptions.ShowMembersOfUsedType);
			return input;
		}

		private ICodeSource? ReadCodeSource(string codeSourceLocation)
		{
			if (codeSourceLocation.IsNullOrWhiteSpace())
				return null;

			if (!File.Exists(codeSourceLocation))
				throw new ArgumentException($"Code source to use in validation is not found at {codeSourceLocation}");

			string fullPath = Path.GetFullPath(codeSourceLocation);
			ICodeSource codeSource = CreateCodeSource(fullPath);
			return codeSource;
		}

		private ICodeSource CreateCodeSource(string codeSourceLocation)
		{
			string extension = Path.GetExtension(codeSourceLocation);

			return extension switch
			{
				CommonConstants.ProjectFileExtension  => new ProjectCodeSource(codeSourceLocation),
				CommonConstants.SolutionFileExtension => new SolutionCodeSource(codeSourceLocation),
				_ => throw new ArgumentException($"Not supported code source {codeSourceLocation}. You can specify only C# projects (*.csproj) and solutions (*.sln) as code sources.")
			};
		}

		private GroupingMode ReadGroupingMode(string? rawGroupingLocation)
		{
			if (rawGroupingLocation.IsNullOrWhiteSpace())
				return GroupingMode.None;
			
			string rawGroupingLocationUppered = rawGroupingLocation.ToUpperInvariant();
			bool groupByNamespaces 			  = rawGroupingLocationUppered.Contains('N');
			bool groupByTypes 				  = rawGroupingLocationUppered.Contains('T');

			GroupingMode grouping = groupByNamespaces
				? GroupingMode.Namespaces
				: GroupingMode.None;

			if (groupByTypes)
				grouping |= GroupingMode.Types;

			return grouping;
		}
	}
}
