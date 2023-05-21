using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CommandLine;

using CoreCompatibilyzer.Runner.Analysis;
using CoreCompatibilyzer.Runner.Analysis.CodeSources;
using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Runner.Utils;
using CoreCompatibilyzer.Utils.Common;

using Serilog;
using Serilog.Events;

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
				Log.Error(e, "An unhandled runtime error was encountered during the validation.");
				return RunResult.RunTimeError.ToExitCode();
			}
		}

		private static async Task<RunResult> RunValidationWithParsedOptionsAsync(CommandLineOptions commandLineOptions)
		{
			if (!TryInitalizeSerilog(commandLineOptions))
				return RunResult.RunTimeError;

			using var consoleCancellationSubscription = new ConsoleCancellationSubscription(Log.Logger);
			AppAnalysisContext? analysisContext = CreateAnalysisContextFromCommandLineArguments(commandLineOptions);

			if (analysisContext == null)
				return RunResult.RunTimeError;

			var analyzer = new SolutionAnalysisRunner();
			var analysisResult = await analyzer.RunAnalysisAsync(analysisContext, consoleCancellationSubscription.CancellationToken);

			OutputValidationResult(analysisResult, analysisContext.CodeSource.Type);

			return analysisResult;
		}

		private static bool TryInitalizeSerilog(CommandLineOptions commandLineOptions)
		{
			try
			{
				var loggerConfiguration = new LoggerConfiguration().WriteTo.Console();
																   
				LogEventLevel logLevel = LogEventLevel.Information;

				if (!commandLineOptions.Verbosity.IsNullOrWhiteSpace() && 
					!Enum.TryParse(commandLineOptions.Verbosity, ignoreCase: true, out logLevel))
				{
					Console.WriteLine($"The logger verbosity value \"{commandLineOptions.Verbosity}\" is not supported. " +
									  "Use help to see the list of allowed verbosity values.");
					return false;
				}

				loggerConfiguration = loggerConfiguration.MinimumLevel.Is(logLevel)
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

		private static AppAnalysisContext? CreateAnalysisContextFromCommandLineArguments(CommandLineOptions commandLineOptions)
		{
			try
			{
				AnalysisContextBuilder analysisContextBuilder = new AnalysisContextBuilder();
				AppAnalysisContext analysisContext = analysisContextBuilder.CreateContext(commandLineOptions);
				return analysisContext;
			}
			catch (Exception e)
			{
				Log.Error(e, "An error happened during the processing of input command line arguments and initialization of analysis context.");
				return null;
			}
		}

		private static Task<RunResult> OnParsingErrorsAsync(IEnumerable<Error> parsingErrors)
		{
			foreach (var error in parsingErrors) 
			{
				Log.Error("Parsing error type: {ErrorType}.", error.Tag);
			}

			return Task.FromResult(RunResult.RunTimeError);
		}

		[SuppressMessage("CodeQuality", "Serilog004:Constant MessageTemplate verifier", Justification = "No need for structured loghing here")]
		private static void OutputValidationResult(RunResult runResult, CodeSourceType codeSourceType)
		{
			switch (runResult)
			{
				case RunResult.Success:
					Log.Information("The validation passed successfully!");
					return;
				case RunResult.RequirementsNotMet:
					string codeSourceTypeName = codeSourceType switch
					{
						CodeSourceType.Solution => "solution",
						CodeSourceType.Project => "project",
						_ => "code"
					};

					Log.Error($"The validation is finished. The validated {codeSourceTypeName} did not pass the validation.");
					return;
				case RunResult.Cancelled:
					Log.Warning("The validation was cancelled.");
					return;
				case RunResult.RunTimeError:
					Log.Error("A runtime error was encountered during the validation.");
					return;
			}
		}
	}
}