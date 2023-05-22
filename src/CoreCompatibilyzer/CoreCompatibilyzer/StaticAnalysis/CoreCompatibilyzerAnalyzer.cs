using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Runtime.Versioning;

using CoreCompatibilyzer.Utils.Common;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;


namespace CoreCompatibilyzer.StaticAnalysis.NotCompatibleWorkspaces
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class CoreCompatibilyzerAnalyzer : DiagnosticAnalyzer
	{
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		   ImmutableArray.Create(Descriptors.CoreCompat1001_ApiNotCompatibleWithDotNetCore);

		public sealed override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze); // We want to analyze for compatibility even generated code
			context.EnableConcurrentExecution();

			//context.RegisterCompilationStartAction(c => c.Compilation.)


			RegisterAnalysisActions(context);
		}
	}
}
