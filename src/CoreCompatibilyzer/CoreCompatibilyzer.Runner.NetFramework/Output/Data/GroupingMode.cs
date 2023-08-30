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
        Namespaces = 0xb0001,

        /// <summary>
        /// Group API calls by types.
        /// </summary>
        Types = 0xb0010,

        /// <summary>
        /// Group API calls by API.
        /// </summary>
        Apis = 0xb0100
    }

    internal static class GroupingModeExtensions
    {
        public static bool HasGrouping(this GroupingMode groupingMode, GroupingMode groupingToCheck) =>
            (groupingMode & groupingToCheck) == groupingToCheck;
    }
}