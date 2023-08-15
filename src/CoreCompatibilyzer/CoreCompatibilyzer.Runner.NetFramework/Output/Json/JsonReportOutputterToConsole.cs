using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CoreCompatibilyzer.Runner.Output.Json
{
	/// <summary>
	/// JSON report outputter to console.
	/// </summary>
	internal class JsonReportOutputterToConsole : JsonReportOutputterBase
	{
		public override void Dispose() { }

		protected override void OutputReportText(string serializedReport) => Console.WriteLine(serializedReport);
	}
}