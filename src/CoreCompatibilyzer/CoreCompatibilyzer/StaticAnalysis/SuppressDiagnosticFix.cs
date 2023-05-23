using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using CoreCompatibilyzer.Utils.Common;
using CoreCompatibilyzer.Utils.Resources;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreCompatibilyzer.StaticAnalysis
{
	[Shared]
	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class SuppressDiagnosticFix : CodeFixProvider
	{
		private const string SuppressionCommentFormat = @"// CoreCompatibilyzer disable once {0} {1} {2}";
		private static readonly ImmutableArray<string> _fixableDiagnosticIds;

		static SuppressDiagnosticFix()
		{
			Type diagnosticsType = typeof(Descriptors);
			var propertiesInfo = diagnosticsType.GetRuntimeProperties();

			_fixableDiagnosticIds = propertiesInfo
				.Where(property => property.PropertyType == typeof(DiagnosticDescriptor))
				.Select(property =>
				{
					var descriptor = property.GetValue(null) as DiagnosticDescriptor;
					return descriptor?.Id;
				})
				.Where(id => id != null)
				.ToImmutableArray()!;
		}

		public override ImmutableArray<string> FixableDiagnosticIds => _fixableDiagnosticIds;

		public override FixAllProvider? GetFixAllProvider() => null;        //explicitly disable fix all support

		public override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			foreach (var diagnostic in context.Diagnostics)
			{
				RegisterCodeActionForDiagnostic(diagnostic, context);
			}

			return Task.CompletedTask;
		}

		private void RegisterCodeActionForDiagnostic(Diagnostic diagnostic, CodeFixContext context)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			string suppressWithCommentCodeActionFormat = nameof(Diagnostics.SuppressDiagnosticWithCommentCodeActionTitle).GetLocalized().ToString();
			string suppressWithCommentCodeActionName   = string.Format(suppressWithCommentCodeActionFormat, diagnostic.Id);
			string equivalenceKey					   = suppressWithCommentCodeActionName + diagnostic.Id;
			var suppressWithCommentCodeAction		   = CodeAction.Create(suppressWithCommentCodeActionName,
																		   cToken => AddSuppressionCommentAsync(context, diagnostic, cToken),
																		   equivalenceKey);
			context.RegisterCodeFix(suppressWithCommentCodeAction, diagnostic);
		}

		private async Task<Document> AddSuppressionCommentAsync(CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var document = context.Document;
			SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			SyntaxNode? reportedNode = root?.FindNode(context.Span);

			if (diagnostic == null || reportedNode == null)
				return document;

			cancellationToken.ThrowIfCancellationRequested();

			var (diagnosticShortName, diagnosticJustification) = GetDiagnosticShortNameAndJustification(diagnostic);

			if (diagnosticShortName.IsNullOrWhiteSpace())
				return document;

			string suppressionComment = string.Format(SuppressionCommentFormat, diagnostic.Id, diagnosticShortName, diagnosticJustification);
			var suppressionCommentTrivias = new SyntaxTrivia[]
			{
				SyntaxFactory.SyntaxTrivia(SyntaxKind.SingleLineCommentTrivia, suppressionComment),
				SyntaxFactory.ElasticEndOfLine("")
			};

			SyntaxNode? nodeToPlaceComment = reportedNode;

			while (nodeToPlaceComment is not (StatementSyntax or MemberDeclarationSyntax or UsingDirectiveSyntax or null))
			{
				nodeToPlaceComment = nodeToPlaceComment.Parent;
			}

			if (nodeToPlaceComment == null)
				return document;

			SyntaxTriviaList leadingTrivia = nodeToPlaceComment.GetLeadingTrivia();
			SyntaxNode? modifiedRoot;

			if (leadingTrivia.Count > 0)
				modifiedRoot = root!.InsertTriviaAfter(leadingTrivia.Last(), suppressionCommentTrivias);
			else
			{
				var nodeWithSuppressionComment = nodeToPlaceComment.WithLeadingTrivia(suppressionCommentTrivias);
				modifiedRoot = root!.ReplaceNode(nodeToPlaceComment, nodeWithSuppressionComment);
			}

			return modifiedRoot != null
				? document.WithSyntaxRoot(modifiedRoot)
				: document;
		}

		private (string? DiagnosticShortName, string? DiagnosticJustification) GetDiagnosticShortNameAndJustification(Diagnostic diagnostic)
		{
			string[]? customTags = diagnostic.Descriptor.CustomTags?.ToArray();

			if (customTags.IsNullOrEmpty())
				return default;

			string diagnosticShortName = customTags[0];
			string diagnosticJustification = customTags.Length > 1
				? customTags[1]
				: DiagnosticsDefaultJustification.Default;

			return (diagnosticShortName, diagnosticJustification);
		}
	}
}