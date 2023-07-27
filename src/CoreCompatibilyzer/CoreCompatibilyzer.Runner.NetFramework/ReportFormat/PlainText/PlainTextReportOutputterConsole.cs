using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

using static System.Net.Mime.MediaTypeNames;

namespace CoreCompatibilyzer.Runner.NetFramework.ReportFormat.PlainText
{
	/// <summary>
	/// The base class for the report outputter in the plain text format.
	/// </summary>
	internal class PlainTextReportOutputterConsole : PlainTextReportOutputterBase
	{
		protected override void WriteLine() => Console.WriteLine();

		protected override void WriteLine(string text) => Console.WriteLine(text);

		protected override void WriteAllApisTitle(string allApisTitle) =>
			OutputTitle(allApisTitle, ConsoleColor.DarkCyan);

		protected override void WriteNamespaceTitle(string namespaceTitle) =>
			OutputTitle(namespaceTitle, ConsoleColor.DarkCyan);

		protected override void WriteTypeTitle(string typeTitle) =>
			OutputTitle(typeTitle, ConsoleColor.Magenta);

		protected override void WriteTypeMembersTitle(string typeMembersTitle) =>
			OutputTitle(typeMembersTitle, ConsoleColor.Gray);

		protected override void WriteApiTitle(string apiTitle) =>
			 OutputTitle(apiTitle, ConsoleColor.Cyan);

		protected override void WriteUsagesTitle(string usagesTitle) =>
			OutputTitle(usagesTitle, ConsoleColor.Blue);

		protected override void WriteFlatApiUsage(string fullApiName, string location)
		{ 
			var oldColor = Console.ForegroundColor;

			try
			{
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write(fullApiName);
			}
			finally
			{
				Console.ForegroundColor = oldColor;
			}

			Console.WriteLine("; " + location);
		}

		private void OutputTitle(string text, ConsoleColor color)
		{
			var oldColor = Console.ForegroundColor;

			try
			{
				Console.ForegroundColor = color;
				Console.WriteLine(text);
			}
			finally
			{
				Console.ForegroundColor = oldColor;
			}
		}
	}
}
