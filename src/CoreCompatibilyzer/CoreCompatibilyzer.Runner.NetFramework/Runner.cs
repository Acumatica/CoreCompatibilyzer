using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

using CoreCompatibilyzer.Utils.Runner;

using Serilog;

namespace CoreCompatibilyzer.Runner.NetFramework
{
	internal class Runner
	{
		static int Main(string[] args)
		{
			try
			{
				var validationResult = RunValidation();
				return validationResult.ToExitCode();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return RunResult.RunTimeError.ToExitCode();
			}
		}

		private static void ConfigureLogger()
		{
			var loggerConfiguration = new LoggerConfiguration().WriteTo.Console()
															   .Enrich.FromLogContext();
			Log.Logger = loggerConfiguration.CreateLogger();
		}

		private static RunResult RunValidation()
		{

		}
	}
}
