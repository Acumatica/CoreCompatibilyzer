using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

using CoreCompatibilyzer.Utils.Common;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CoreCompatibilyzer.Utils.Roslyn.Suppression
{
	public static class SuppressionManager
	{
		private static readonly Regex _suppressPattern = new Regex(@"CoreCompatibilyzer\s+disable\s+once\s+(\w+)\s+(\w+)", RegexOptions.Compiled);
		private static readonly object _lock = new object();

		/// <summary>
		/// Gets or sets a flag indicating whether the suppression mechanism is enabled.
		/// </summary>
		/// <remarks>
		/// This flag is not thread safe. It's purpose is to allow console runner to disable suppression mechanism if needed. 
		/// Don't use it in concurrent environment.
		/// </remarks>
		/// <value>
		/// True if suppression is enabled.
		/// </value>
		public static bool UseSuppression
		{
			get;
			set;
		} = true;

		public static void ReportDiagnosticWithSuppressionCheck(this in SymbolAnalysisContext context, Diagnostic diagnostic) =>
			ReportDiagnosticWithSuppressionCheck(diagnostic, context.ReportDiagnostic, context.CancellationToken);

		public static void ReportDiagnosticWithSuppressionCheck(this in SyntaxNodeAnalysisContext context, Diagnostic diagnostic) =>
			ReportDiagnosticWithSuppressionCheck(diagnostic, context.ReportDiagnostic, context.CancellationToken);

		public static void ReportDiagnosticWithSuppressionCheck(this in CodeBlockAnalysisContext context, Diagnostic diagnostic) => 
			ReportDiagnosticWithSuppressionCheck(diagnostic, context.ReportDiagnostic, context.CancellationToken);

		private static void ReportDiagnosticWithSuppressionCheck(Diagnostic diagnostic, Action<Diagnostic> reportDiagnostic, 
																 CancellationToken cancellation)
		{
			diagnostic.ThrowIfNull(nameof(diagnostic));
			cancellation.ThrowIfCancellationRequested();

			if (!UseSuppression || !IsSuppressedByComment(diagnostic, cancellation))
			{
				reportDiagnostic(diagnostic);
			}
		}

		private static bool IsSuppressedByComment(Diagnostic diagnostic, CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();

			SyntaxNode? root = diagnostic.Location.SourceTree?.GetRoot(cancellation);
			SyntaxNode? node = root?.FindNode(diagnostic.Location.SourceSpan);
			bool containsComment = false;
			string? shortName = diagnostic.Descriptor.CustomTags.FirstOrDefault().NullIfWhiteSpace();

			// Climb to the hill. Looking for comment on parents nodes.
			while (node != null && node != root)
			{
				cancellation.ThrowIfCancellationRequested();

				containsComment = CheckSuppressionCommentOnNode(diagnostic, shortName, node);

				if (node is (StatementSyntax or MemberDeclarationSyntax or UsingDirectiveSyntax) || containsComment)
					break;

				node = node.Parent;
			}

			return containsComment;
		}

		private static bool CheckSuppressionCommentOnNode(Diagnostic diagnostic, string? diagnosticShortName, SyntaxNode node)
		{
			var trivia = node.GetLeadingTrivia();

			if (trivia.Count == 0)
				return false;

			var successfulMatch = trivia.Where(x => x.IsKind(SyntaxKind.SingleLineCommentTrivia))
									   .Select(trivia => _suppressPattern.Match(trivia.ToString()))
									   .FirstOrDefault(match => match.Success && diagnostic.Id == match.Groups[1].Value &&
																(diagnosticShortName == null || diagnosticShortName == match.Groups[2].Value));
			return successfulMatch != null;
		}
	}
}
