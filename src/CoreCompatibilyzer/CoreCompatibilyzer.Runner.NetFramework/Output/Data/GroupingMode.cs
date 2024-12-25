using System;

namespace CoreCompatibilyzer.Runner.Output.Data
{
	/// <summary>
	/// Report grouping modes.
	/// </summary>
	[Flags]
	internal enum GroupingMode
	{
		/// <summary>
		/// No grouping is specified for the report.
		/// </summary>
		None = 0b0000,

		/// <summary>
		/// Group API calls by namespaces.
		/// </summary>
		Namespaces = 0b0001,

		/// <summary>
		/// Group API calls by types.
		/// </summary>
		Types = 0b0010,

		/// <summary>
		/// Group API calls by API.
		/// </summary>
		Apis = 0b0100,

		/// <summary>
		/// Group API calls by source file.
		/// </summary>
		Files = 0b1000

		//  TODO: add other grouping modes, not only by files
	}

	internal static class GroupingModeExtensions
	{
		public static bool HasGrouping(this GroupingMode groupingMode, GroupingMode groupingToCheck) =>
			(groupingMode & groupingToCheck) == groupingToCheck;
	}
}