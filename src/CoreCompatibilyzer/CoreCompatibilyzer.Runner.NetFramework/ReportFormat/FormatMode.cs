using System;

namespace CoreCompatibilyzer.Runner.ReportFormat
{
	/// <summary>
	/// Report format modes.
	/// </summary>
	internal enum FormatMode
	{
		/// <summary>
		/// Format Mode to output only a shortened list of used banned APIs.
		/// </summary>
		UsedAPIsOnly,

		/// <summary>
		/// Format Mode to output only a detailed list of used banned APIs with usages locations.
		/// </summary>
		UsedAPIsWithUsages
	}
}
