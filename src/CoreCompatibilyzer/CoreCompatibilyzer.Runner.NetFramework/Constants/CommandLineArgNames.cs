using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreCompatibilyzer.Runner.Constants
{
	internal static class CommandLineArgNames
	{
		public const string CodeSource = "codeSource";

		public const char TargetFrameworkShort = 't';
		public const string TargetFrameworkLong = "target";

		public const char VerbosityShort = 'v';
		public const string VerbosityLong = "verbosity";

		public const string DisableSuppressionMechanism = "noSuppression";
		public const string MSBuildPath = "msBuildPath";

		public const char ReportFormatShort = 'f';
		public const string ReportFormatLong = "format";

		public const char ReportGroupingShort = 'g';
		public const string ReportGroupingLong = "grouping";
	}
}