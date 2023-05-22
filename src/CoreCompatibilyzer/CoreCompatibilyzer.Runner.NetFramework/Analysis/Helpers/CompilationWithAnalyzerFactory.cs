#nullable enable

using System;
using System.Threading;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CoreCompatibilyzer.Runner.Analysis.Helpers
{
	/// <summary>
	/// A <see cref="CompilationWithAnalyzers"/> factory. This class is a workaround for Roslyn inconsistent API for <see cref="CompilationWithAnalyzers"/> creation. <br/>
	/// Roslyn API allows either to pass custom <see cref="CompilationWithAnalyzersOptions"/> options to the instantiated <see cref="CompilationWithAnalyzers"/> instance or
	/// a <see cref="CancellationToken"/> but not both.<br/>
	/// </summary>
	/// <remarks>
	/// See this for more info:<br/>
	/// https://github.com/dotnet/roslyn/issues/41522
	/// </remarks>
	internal static class CompilationWithAnalyzerFactory
	{
		public static CompilationWithAnalyzers WithAnalyzers(this Compilation compilation, CompilationWithAnalyzersOptions compilationWithAnalyzersOptions,
															 ImmutableArray<DiagnosticAnalyzer> diagnosticAnalyzers, CancellationToken cancellation)
		{
			if (cancellation == CancellationToken.None)
				return compilation.WithAnalyzers(diagnosticAnalyzers, compilationWithAnalyzersOptions);

			var bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
			var args = new object[] { compilation, diagnosticAnalyzers, compilationWithAnalyzersOptions, cancellation };
			var compilationWithAnalyzers = 
				Activator.CreateInstance(typeof(CompilationWithAnalyzers), bindingFlags, binder: null, args, culture: null) as CompilationWithAnalyzers;

			return compilationWithAnalyzers ?? throw new InvalidOperationException("Failed to obtain compilation with analyzers");
		}
	}
}
