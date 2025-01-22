using System;
using System.Collections.Generic;
using System.Linq;

using CoreCompatibilyzer.Runner.Output.Data;

namespace CoreCompatibilyzer.Runner.Output.PlainText
{
	/// <summary>
	/// The base class for the report outputter in the plain text format.
	/// </summary>
	internal class PlainTextReportOutputterConsole : PlainTextReportOutputterBase
	{
		public override void Dispose() { }

		protected override void WriteDistinctApisTitle(string titleText, int depth, int itemsCount)
		{
			if (titleText == null)
				return;

			string padding = GetPadding(depth);
			string suffix  = itemsCount > 0 ? ":" : string.Empty;

			WriteAllDistinctApisTitle($"{padding}{titleText}(Count: {itemsCount}){suffix}");
		}

		protected override void WriteTitle(in Title? title, int depth, int itemsCount, int distinctApisCount, bool hasContent)
		{
			if (title == null)
				return;

			string padding = GetPadding(depth);
			string suffix = hasContent ? ":" : string.Empty;
			string titleWithPadding = $"{padding}{title.Value.Text}(Count: {itemsCount}, Distinct APIs: {distinctApisCount}){suffix}";

			switch (title?.Kind)
			{
				case TitleKind.File:
					WriteFileName(titleWithPadding);
					return;
				case TitleKind.Namespace:
					WriteNamespaceTitle(titleWithPadding);
					return;
				case TitleKind.Type:
					WriteTypeTitle(titleWithPadding);
					return;
				case TitleKind.Members:
					WriteMembersTitle(titleWithPadding);
					return;
				case TitleKind.Api:
					WriteApiTitle(titleWithPadding);
					return;
				case TitleKind.AllApis:
					WriteAllApisTitle(titleWithPadding);
					return;
				case TitleKind.Usages:
					WriteUsagesTitle($"{padding}{title.Value.Text}{suffix}");
					return;
				default:
					WriteLine(titleWithPadding);
					return;
			}
		}

		protected override void WriteLine() => Console.WriteLine();

		protected override void WriteLine(string text) => Console.WriteLine(text);

		protected override void WriteLine(in Line line, int depth)
		{
			switch (line.Spans.Length)
			{
				case 0:
					WriteLine();
					return;

				case 2:
					WriteFlatApiUsage(depth, line.Spans[0].ToString(), line.Spans[1].ToString());
					return;
					
				case 1:
				default:
					string padding = GetPadding(depth);
					WriteLine(padding + line.ToString());
					return;
			}
		}

		private void WriteFlatApiUsage(int depth, string fullApiName, string location)
		{ 
			string padding = GetPadding(depth);
			var oldColor   = Console.ForegroundColor;

			try
			{
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write(padding + fullApiName);
			}
			finally
			{
				Console.ForegroundColor = oldColor;
			}

			Console.Write($": {location}{Environment.NewLine}");
		}

		private void WriteAllApisTitle(string allApisTitle) =>
			OutputTitle(allApisTitle, ConsoleColor.DarkCyan);

		private void WriteAllDistinctApisTitle(string allDistinctApisTitle) =>
			OutputTitle(allDistinctApisTitle, ConsoleColor.DarkCyan);

		private void WriteFileName(string fileName) =>
			OutputTitle(fileName, ConsoleColor.DarkMagenta);

		private void WriteNamespaceTitle(string namespaceTitle) =>
			OutputTitle(namespaceTitle, ConsoleColor.DarkCyan);

		private void WriteTypeTitle(string typeTitle) =>
			OutputTitle(typeTitle, ConsoleColor.Magenta);

		private void WriteMembersTitle(string typeMembersTitle) =>
			OutputTitle(typeMembersTitle, ConsoleColor.Yellow);

		private void WriteApiTitle(string apiTitle) =>
			 OutputTitle(apiTitle, ConsoleColor.Cyan);

		private void WriteUsagesTitle(string usagesTitle) =>
			OutputTitle(usagesTitle, ConsoleColor.Blue);

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
