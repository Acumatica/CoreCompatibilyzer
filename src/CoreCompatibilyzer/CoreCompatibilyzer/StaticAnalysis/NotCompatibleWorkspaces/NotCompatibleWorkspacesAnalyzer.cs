using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using static System.Math;
using X = System.Collections;

namespace CoreCompatibilyzer.StaticAnalysis.NotCompatibleWorkspaces
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NotCompatibleWorkspacesAnalyzer : CoreCompatibilyzerAnalyzerBase
	{
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create(Descriptors.CoreCompat1001_WorkspaceNotCompatibleWithCore);

		protected override void RegisterAnalysisActions(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(AnalyzeSyntaxTreeForUsingDirectives, SyntaxKind.CompilationUnit);
		}

        private static void AnalyzeSyntaxTreeForUsingDirectives(SyntaxNodeAnalysisContext context)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			if (context.Node is not CompilationUnitSyntax compilationUnit)
				return;

            
			


			//var diagnostic = Diagnostic.Create(Descriptors.CoreCompat1001_WorkspaceNotCompatibleWithCore, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

			//context.ReportDiagnostic(diagnostic);
		}
    }
}
