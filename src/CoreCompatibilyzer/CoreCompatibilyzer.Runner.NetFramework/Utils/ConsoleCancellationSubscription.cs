using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Serilog;

namespace CoreCompatibilyzer.Runner.Utils
{
	internal class ConsoleCancellationSubscription : IDisposable
	{
		private bool _isDisposed;
		private readonly CancellationTokenSource _cancellationTokenSource;
		private readonly ILogger? _logger;
		private bool _oldTreatControlCAsInput;

		public CancellationToken CancellationToken =>
			_isDisposed
				? throw new ObjectDisposedException(nameof(ConsoleCancellationSubscription))
				: _cancellationTokenSource.Token;

		public bool IsCancellationRequested => CancellationToken.IsCancellationRequested;

		public ConsoleCancellationSubscription(ILogger? logger)
		{
			_logger = logger;
			
			_logger?.Debug("Subscribing on Console cancellation events.");

			_cancellationTokenSource = new CancellationTokenSource();
			_oldTreatControlCAsInput = Console.TreatControlCAsInput;
			Console.TreatControlCAsInput = false;
			Console.CancelKeyPress += Console_CancelKeyPress;
		}

		private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
		{
			string keyCombination = e.SpecialKey == ConsoleSpecialKey.ControlC
				? "Ctrl + C"
				: "Ctrl + Break";

			_logger?.Warning("Cancelling the validation because {KeyCombination} was pressed.", keyCombination);
			
			_cancellationTokenSource.Cancel();
			e.Cancel = true;
		}

		public void Dispose()
		{
			if (_isDisposed)
				return;

			try
			{
				_logger?.Debug("Disposing the console cancellation subscription.");
				Console.CancelKeyPress -= Console_CancelKeyPress;
				Console.TreatControlCAsInput = _oldTreatControlCAsInput;

				_cancellationTokenSource.Dispose();
				_isDisposed = true;
			}
			catch (Exception e)
			{
				_logger?.Error(e, $"An error happened during the disposal of the console cancellation subscription.");
			}
		}
	}
}
