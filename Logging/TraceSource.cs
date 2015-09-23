namespace StockSharp.Logging
{
	using System;
	using System.Diagnostics;

	using Ecng.Common;

	/// <summary>
	/// The logs source which receives information from <see cref="Trace"/>.
	/// </summary>
	public class TraceSource : BaseLogSource
	{
		private class TraceListenerEx : TraceListener
		{
			private readonly TraceSource _source;

			public TraceListenerEx(TraceSource source)
			{
				if (source == null)
					throw new ArgumentNullException("source");

				_source = source;
			}

			public override void Write(string message)
			{
				_source.RaiseDebugLog(message);
			}

			public override void WriteLine(string message)
			{
				_source.RaiseDebugLog(message);
			}

			public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
			{
				var level = ToStockSharp(eventType);

				if (level == null)
					return;

				_source.RaiseLog(new LogMessage(_source, TimeHelper.NowWithOffset, level.Value, message));
			}

			public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
			{
				var level = ToStockSharp(eventType);

				if (level == null)
					return;

				_source.RaiseLog(new LogMessage(_source, TimeHelper.NowWithOffset, level.Value, format, args));
			}

			private static LogLevels? ToStockSharp(TraceEventType eventType)
			{
				switch (eventType)
				{
					case TraceEventType.Critical:
					case TraceEventType.Error:
						return LogLevels.Error;

					case TraceEventType.Warning:
						return LogLevels.Warning;

					case TraceEventType.Information:
						return LogLevels.Info;

					case TraceEventType.Verbose:
						return LogLevels.Debug;

					case TraceEventType.Start:
					case TraceEventType.Stop:
					case TraceEventType.Suspend:
					case TraceEventType.Resume:
					case TraceEventType.Transfer:
						return null;
					default:
						throw new ArgumentOutOfRangeException("eventType");
				}
			}
		}

		private void RaiseDebugLog(string message)
		{
			RaiseLog(new LogMessage(this, TimeHelper.NowWithOffset, LogLevels.Debug, message));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TraceSource"/>.
		/// </summary>
		public TraceSource()
		{
			Trace.Listeners.Add(new TraceListenerEx(this));
		}

		/// <summary>
		/// Name.
		/// </summary>
		public override string Name
		{
			get
			{
				return "Trace";
			}
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			Trace.Listeners.Remove(new TraceListenerEx(this));
			base.DisposeManaged();
		}
	}
}