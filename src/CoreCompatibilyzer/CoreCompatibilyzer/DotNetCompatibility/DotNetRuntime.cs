using System;

namespace CoreCompatibilyzer.DotNetCompatibility
{
	public enum DotNetRuntime
	{
		DotNetFramework,

		DotNetStandard20,   // For an MVP let's support .Net Standard 2.0 and higher
		DotNetStandard21,

		DotNetCore21,       // For an MVP let's start with 2.1 version
		DotNetCore22,
		DotNetCore30,
		DotNetCore31,
		DotNet5,
		DotNet6,
		DotNet7,
		DotNet8
	}


	public static class DotNetRuntimeExtension
	{
		public static bool IsDotNetStandard(this DotNetRuntime runtime) =>
			runtime is DotNetRuntime.DotNetStandard20 or DotNetRuntime.DotNetStandard21;
	}
}
