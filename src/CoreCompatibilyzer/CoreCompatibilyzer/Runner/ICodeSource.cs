using System;

namespace CoreCompatibilyzer.Runner
{
	public interface ICodeSource
	{
		CodeSourceType Type { get; }

		string Location { get; }
	}
}
