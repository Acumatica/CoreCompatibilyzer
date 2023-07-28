using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreCompatibilyzer.Runner.Constants
{
	internal static class CommandLineArgNames
	{
		public const string CodeSource = "codeSource";

		public const char VerbosityShort = 'v';
		public const string VerbosityLong = "verbosity";

		public const string DisableSuppressionMechanism = "noSuppression";
		public const string MSBuildPath = "msBuildPath";

		public const string IncludeApiUsages = "withUsages";
		public const string ShowMembersOfUsedType = "showMembersOfUsedType";
		public const string OutputAbsolutePathsToUsages = "outputAbsolutePaths";

		public const char ReportGroupingShort = 'g';
		public const string ReportGroupingLong = "grouping";

		public const char OutputFileShort = 'f';
		public const string OutputFileLong = "file";

		public const string OutputFormat = "format";
	}
}