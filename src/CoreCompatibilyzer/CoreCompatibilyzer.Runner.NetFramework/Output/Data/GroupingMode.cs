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
        Namespaces = ByUsedApi | 0xb0001,

        /// <summary>
        /// Group API calls by types.
        /// </summary>
        Types = ByUsedApi | 0xb0010,

        /// <summary>
        /// Group API calls by API.
        /// </summary>
        Apis = ByUsedApi | 0xb0100,

        ByUsedApi = 0b1000,

        // todo: xml

        Files = BySource | 0b0001_0000,

        BySource = 0b1000_0000,

    }

    internal static class GroupingModeExtensions
    {
        public static bool HasGrouping(this GroupingMode groupingMode, GroupingMode groupingToCheck) =>
            (groupingMode & groupingToCheck) == groupingToCheck;
    }
}