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

		public const char ReportGroupingShort = 'g';
		public const string ReportGroupingLong = "grouping";
	}
}