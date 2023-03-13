using System.Collections.Generic;

namespace CoreCompatibilyzer.DotNetCompatibility
{
	public class DotNetRunTimeComparer : IComparer<DotNetRuntime>
	{
		public static readonly DotNetRunTimeComparer Instance = new DotNetRunTimeComparer();

		public int Compare(DotNetRuntime x, DotNetRuntime y)
		{		
			if (x == y)
				return 0;

			bool isNetStandardX = x.IsDotNetStandard();
			bool isNetStandardY = y.IsDotNetStandard();

			if (isNetStandardX && isNetStandardY)
				return TrivialCompare(x, y);
			else if (isNetStandardX)
			{
				if (x == DotNetRuntime.DotNetStandard20)
					return CompareWithNetStandard20(y);
				else
					return CompareWithNetStandard21(y);
			}
			else if (isNetStandardY) 
			{
				if (y == DotNetRuntime.DotNetStandard20)
					return -CompareWithNetStandard20(x);
				else
					return -CompareWithNetStandard21(x);
			}
			else
				return TrivialCompare(x, y);
		}

		private static int TrivialCompare(DotNetRuntime x, DotNetRuntime y)
		{
			if (x > y)
				return 1;
			else if (x < y)
				return -1;
			else
				return 0;
		}

		private static int CompareWithNetStandard20(DotNetRuntime runTimeToCompare)
		{
			switch (runTimeToCompare)
			{
				case DotNetRuntime.DotNetFramework:
					return 1;
				case DotNetRuntime.DotNetStandard20:
					return 0;
				default:
					return -1;
			}
		}

		private static int CompareWithNetStandard21(DotNetRuntime runTimeToCompare)
		{
			switch (runTimeToCompare)
			{
				case DotNetRuntime.DotNetFramework:
				case DotNetRuntime.DotNetStandard20:
				case DotNetRuntime.DotNetCore21:
				case DotNetRuntime.DotNetCore22:
					return 1;
				case DotNetRuntime.DotNetStandard21:
					return 0;
				default:
					return -1;
			}
		}
	}
}
