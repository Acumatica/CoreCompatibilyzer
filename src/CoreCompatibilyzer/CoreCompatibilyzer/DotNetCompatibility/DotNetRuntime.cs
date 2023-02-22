using System;

namespace CoreCompatibilyzer.DotNetCompatibility
{
	public enum DotNetRuntime
	{
		DotNetFramework,
		DotNetCore21,       // For an MVP let's start with 2.1 version
		DotNetCore22,
		DotNetCore30,
		DotNetCore31,
		DotNet5,
		DotNet6
	}
}
