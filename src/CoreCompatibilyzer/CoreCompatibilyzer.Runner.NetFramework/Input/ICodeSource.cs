using System;

namespace CoreCompatibilyzer.Runner.Input
{
	internal interface ICodeSource
	{
		CodeSourceType Type { get; }

		string Location { get; }
	}
}
