using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CommandLine;

using CoreCompatibilyzer.Runner.Analysis;
using CoreCompatibilyzer.Runner.Input;

using Serilog;

namespace CoreCompatibilyzer.Runner.NetFramework
{
    internal class Runner
	{
		static async Task<int> Main(string[] args)
		{
			try
			{
				ParserResult<CommandLineOptions> argsParsingResult = Parser.Default.ParseArguments<CommandLineOptions>(args);
				RunResult runResult = await argsParsingResult.MapResult(parsedFunc: RunValidationWithParsedOptionsAsync,
																		notParsedFunc: OnParsingErrorsAsync);
				return runResult.ToExitCode();
			}
			catch (Exception e)
			{
				Log.Error(e, "An unhandled runtime error was encountered during the validation");
				return RunResult.RunTimeError.ToExitCode();
			}
		}

		private static async Task<RunResult> RunValidationWithParsedOptionsAsync(CommandLineOptions commandLineOptions)
		{
			if (!TryInitalizeSerilog(commandLineOptions))
				return RunResult.RunTimeError;

			AnalysisContext? analysisContext = CreateAnalysisContextFromCommandLineArguments(commandLineOptions);

			if (analysisContext == null)
				return RunResult.RunTimeError;

			var analyzer = new CompatibilityAnalysisRunner();
			var analysisResult = await analyzer.Analyze(analysisContext, CancellationToken.None);
			return analysisResult;
		}

		private static bool TryInitalizeSerilog(CommandLineOptions commandLineOptions)
		{
			try
			{
				var loggerConfiguration = new LoggerConfiguration().WriteTo.Console()
																   .Enrich.FromLogContext();
				Log.Logger = loggerConfiguration.CreateLogger();
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);   // Log failed serilog initialization directly to console
				return false;
			}
		}

		private static AnalysisContext? CreateAnalysisContextFromCommandLineArguments(CommandLineOptions commandLineOptions)
		{
			try
			{
				AnalysisContextBuilder analysisContextBuilder = new AnalysisContextBuilder();
				AnalysisContext analysisContext = analysisContextBuilder.CreateContext(commandLineOptions);
				return analysisContext;
			}
			catch (Exception e)
			{
				Log.Error(e, "An error happened during the processing of input command line arguments and initialization of analysis context");
				return null;
			}
		}

		private static Task<RunResult> OnParsingErrorsAsync(IEnumerable<Error> parsingErrors)
		{
			foreach (var error in parsingErrors) 
			{
				Log.Error("Parsing error type: {ErrorType}", error.Tag);
			}

			return Task.FromResult(RunResult.RunTimeError);
		}
	}
}