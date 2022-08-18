using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Kicker.UI
{
	public class DebugLoggerProvider : ILoggerProvider
	{
		public void Dispose()
		{
		}

		public ILogger CreateLogger(string categoryName)
		{
			return new DebugLogger();
		}

		private class DebugLogger : ILogger
		{
			public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
				Func<TState, Exception, string> formatter)
			{
				Debug.WriteLine(formatter(state, exception));
			}

			public bool IsEnabled(LogLevel logLevel)
			{
				return true;
			}

			public IDisposable BeginScope<TState>(TState state)
			{
				return null;
			}
		}
	}
}
