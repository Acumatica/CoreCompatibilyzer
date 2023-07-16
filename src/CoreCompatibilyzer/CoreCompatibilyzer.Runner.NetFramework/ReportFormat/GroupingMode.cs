using System;

namespace CoreCompatibilyzer.Runner.ReportFormat
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
		None = 0,

		/// <summary>
		/// Group API by namespaces.
		/// </summary>
		Namespaces = 0xb001,

		/// <summary>
		/// Group API by types.
		/// </summary>
		Types = 0xb010
	}
}