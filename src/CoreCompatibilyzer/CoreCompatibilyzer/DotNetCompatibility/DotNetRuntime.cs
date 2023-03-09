using System;
using System.Collections.Generic;

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

	public class DotNetRunTimeComparer : IComparer<DotNetRuntime>
	{
		public static DotNetRunTimeComparer Instance { get; } = new DotNetRunTimeComparer();

		public int Compare(DotNetRuntime x, DotNetRuntime y)
		{			
			if (x > y)
				return 1;
			else if (x < y)
				return -1;
			else
				return 0;
		}
	}
}
