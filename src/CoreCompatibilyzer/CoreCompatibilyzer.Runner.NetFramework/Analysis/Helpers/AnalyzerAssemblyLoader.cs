#nullable enable

using System;
using System.Reflection;

using Microsoft.CodeAnalysis;

namespace CoreCompatibilyzer.Runner.Analysis.Helpers
{
	internal class AnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
	{
		public void AddDependencyLocation(string fullPath)
		{
		}

		public Assembly LoadFromPath(string fullPath)
		{
			return Assembly.LoadFrom(fullPath);
		}
	}
}
