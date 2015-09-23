namespace StockSharp.Logging
{
	using System;
	using System.Linq;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Configuration;

	/// <summary>
	/// Extension class for <see cref="ILogSource"/>.
	/// </summary>
	public static class LoggingHelper
	{
		/// <summary>
		/// To record a message to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="getMessage">The function returns the text for <see cref="LogMessage.Message"/>.</param>
		public static void AddInfoLog(this ILogReceiver receiver, Func<string> getMessage)
		{
			receiver.AddLog(LogLevels.Info, getMessage);
		}

		/// <summary>
		/// To record a warning to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="getMessage">The function returns the text for <see cref="LogMessage.Message"/>.</param>
		public static void AddWarningLog(this ILogReceiver receiver, Func<string> getMessage)
		{
			receiver.AddLog(LogLevels.Warning, getMessage);
		}

		/// <summary>
		/// To record an error to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="getMessage">The function returns the text for <see cref="LogMessage.Message"/>.</param>
		public static void AddErrorLog(this ILogReceiver receiver, Func<string> getMessage)
		{
			receiver.AddLog(LogLevels.Error, getMessage);
		}

		/// <summary>
		/// To record a message to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="level">The level of the log message.</param>
		/// <param name="getMessage">The function returns the text for <see cref="LogMessage.Message"/>.</param>
		public static void AddLog(this ILogReceiver receiver, LogLevels level, Func<string> getMessage)
		{
			if (receiver == null)
				throw new ArgumentNullException("receiver");

			receiver.AddLog(new LogMessage(receiver, receiver.CurrentTime, level, getMessage));
		}

		/// <summary>
		/// To record a message to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="message">Text message.</param>
		/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
		public static void AddInfoLog(this ILogReceiver receiver, string message, params object[] args)
		{
			receiver.AddMessage(LogLevels.Info, message, args);
		}

		/// <summary>
		/// To record a debugging to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="message">Text message.</param>
		/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
		public static void AddDebugLog(this ILogReceiver receiver, string message, params object[] args)
		{
			receiver.AddMessage(LogLevels.Debug, message, args);
		}

		/// <summary>
		/// To record a warning to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="message">Text message.</param>
		/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
		public static void AddWarningLog(this ILogReceiver receiver, string message, params object[] args)
		{
			receiver.AddMessage(LogLevels.Warning, message, args);
		}

		/// <summary>
		/// To record an error to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="exception">Error detais.</param>
		public static void AddErrorLog(this ILogReceiver receiver, Exception exception)
		{
			receiver.AddErrorLog(exception, null);
		}

		/// <summary>
		/// To record an error to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="exception">Error detais.</param>
		/// <param name="message">Text message.</param>
		public static void AddErrorLog(this ILogReceiver receiver, Exception exception, string message)
		{
			if (receiver == null)
				throw new ArgumentNullException("receiver");

			if (exception == null)
				throw new ArgumentNullException("exception");

			receiver.AddLog(new LogMessage(receiver, receiver.CurrentTime, LogLevels.Error, () =>
			{
				var msg = exception.ToString();
				
				var refExc = exception as ReflectionTypeLoadException;

				if (refExc != null)
				{
					msg += Environment.NewLine
						+ refExc
							.LoaderExceptions
							.Select(e => e.ToString())
							.Join(Environment.NewLine);
				}

				if (message != null)
					msg = message.Put(msg);

				return msg;
			}));
		}

		/// <summary>
		/// To record an error to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="message">Text message.</param>
		/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
		public static void AddErrorLog(this ILogReceiver receiver, string message, params object[] args)
		{
			receiver.AddMessage(LogLevels.Error, message, args);
		}

		private static void AddMessage(this ILogReceiver receiver, LogLevels level, string message, params object[] args)
		{
			if (receiver == null)
				throw new ArgumentNullException("receiver");

			if (level < receiver.LogLevel)
				return;

			receiver.AddLog(new LogMessage(receiver, receiver.CurrentTime, level, message, args));
		}

		/// <summary>
		/// To record an error to the <see cref="LogManager.Application"/>.
		/// </summary>
		/// <param name="error">Error.</param>
		/// <param name="message">Text message.</param>
		public static void LogError(this Exception error, string message = null)
		{
			if (error == null)
				throw new ArgumentNullException("error");

			var manager = ConfigManager.TryGetService<LogManager>();

			if (manager != null)
				manager.Application.AddErrorLog(error, message);
		}

		/// <summary>
		/// Get <see cref="ILogSource.LogLevel"/> for the source. If the value is equal to <see cref="LogLevels.Inherit"/>, then parental source level is taken.
		/// </summary>
		/// <param name="source">The log source.</param>
		/// <returns>Logging level.</returns>
		public static LogLevels GetLogLevel(this ILogSource source)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			do
			{
				var level = source.LogLevel;

				if (level != LogLevels.Inherit)
					return level;

				source = source.Parent;
			}
			while (source != null);
			
			return LogLevels.Inherit;
		}
	}
}