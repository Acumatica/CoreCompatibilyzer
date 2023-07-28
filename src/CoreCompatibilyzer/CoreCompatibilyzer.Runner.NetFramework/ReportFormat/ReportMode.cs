using System;

namespace CoreCompatibilyzer.Runner.Output
{
	/// <summary>
	/// Report format modes.
	/// </summary>
	internal enum ReportMode
	{
		/// <summary>
		/// Report mode to output only a shortened list of used banned APIs.
		/// </summary>
		UsedAPIsOnly,

		/// <summary>
		/// Report mode to output only a detailed list of used banned APIs with usages locations.
		/// </summary>
		UsedAPIsWithUsages
	}
}
