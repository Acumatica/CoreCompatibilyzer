using System;
using System.Collections.Generic;
using System.Text;

using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.Runner.Input
{
	internal class ProjectCodeSource : ICodeSource
	{
		public CodeSourceType Type => CodeSourceType.Project;

		public string Location { get; }

		public ProjectCodeSource(string projectPath)
		{
			Location = projectPath.ThrowIfNullOrWhiteSpace(nameof(projectPath));
		}
	}
}
