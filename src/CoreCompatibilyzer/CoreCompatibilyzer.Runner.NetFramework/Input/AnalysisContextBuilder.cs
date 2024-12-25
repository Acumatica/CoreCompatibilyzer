using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using CoreCompatibilyzer.DotNetRuntimeVersion;
using CoreCompatibilyzer.Runner.Analysis.CodeSources;
using CoreCompatibilyzer.Runner.Constants;
using CoreCompatibilyzer.Runner.Output.Data;
using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.Runner.Input
{
    internal class AnalysisContextBuilder
	{
		public AppAnalysisContext CreateContext(CommandLineOptions commandLineOptions)
		{
			commandLineOptions.ThrowIfNull(nameof(commandLineOptions));

			var codeSource = ReadCodeSource(commandLineOptions.CodeSource) ?? throw new ArgumentException("Code source is not specified");
			ReportMode reportMode = commandLineOptions.IncludeApiUsages 
				? ReportMode.UsedAPIsWithUsages 
				: ReportMode.UsedAPIsOnly;

			GroupingMode groupingMode = ReadGroupingMode(commandLineOptions.ReportGrouping);
			OutputFormat outputFormat = GetOutputFormat(commandLineOptions.OutputFormat.NullIfWhiteSpace());
			var input = new AppAnalysisContext(codeSource, targetRuntime: DotNetRuntime.DotNetCore22, commandLineOptions.DisableSuppressionMechanism,
											   commandLineOptions.MSBuildPath, reportMode, commandLineOptions.IncludeAllDistinctApis, groupingMode, commandLineOptions.ShowMembersOfUsedType,
											   commandLineOptions.OutputFileName, commandLineOptions.OutputAbsolutePathsToUsages, outputFormat);
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
				_ => throw new NotSupportedException(
						$"Not supported code source {codeSourceLocation}. You can specify only C# projects (*.csproj) and solutions (*.sln) as code sources.")
			};
		}

		private GroupingMode ReadGroupingMode(string? rawGroupingLocation)
		{
			if (rawGroupingLocation.IsNullOrWhiteSpace())
				return GroupingMode.None;
			
			string rawGroupingLocationUppered = rawGroupingLocation.ToUpperInvariant();

			bool groupByFiles 	   = rawGroupingLocationUppered.Contains('F');
			bool groupByNamespaces = rawGroupingLocationUppered.Contains('N');
			bool groupByTypes 	   = rawGroupingLocationUppered.Contains('T');
			bool groupByApis 	   = rawGroupingLocationUppered.Contains('A');

			GroupingMode grouping = groupByFiles
				? GroupingMode.Files
				: GroupingMode.None;

			if (groupByNamespaces)
				grouping |= GroupingMode.Namespaces;

			if (groupByTypes)
				grouping |= GroupingMode.Types;

			if (groupByApis)
				grouping |= GroupingMode.Apis;

			return grouping;
		}

		private OutputFormat GetOutputFormat(string? rawOutputFormat)
		{
			const string plainTextFormat = "text";
			const string jsonFormat = "json";

			if (rawOutputFormat == null || plainTextFormat.Equals(rawOutputFormat, StringComparison.OrdinalIgnoreCase))
				return OutputFormat.PlainText;
			else if (jsonFormat.Equals(rawOutputFormat, StringComparison.OrdinalIgnoreCase))
				return OutputFormat.Json;
			else
			{
				throw new NotSupportedException($"Not supported output format \"{rawOutputFormat}\". " +
												$"You can specify only \"{plainTextFormat}\" and \"{jsonFormat}\" values as output formats.");
			}
		}
	}
}