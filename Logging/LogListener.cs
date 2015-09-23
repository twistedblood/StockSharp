namespace StockSharp.Logging
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Localization;

	/// <summary>
	/// The base class that monitors the event <see cref="ILogSource.Log"/> and saves to some storage.
	/// </summary>
	public abstract class LogListener : Disposable, ILogListener
	{
		static LogListener()
		{
			AllWarningFilter = message => message.Level == LogLevels.Warning;
			AllErrorFilter = message => message.Level == LogLevels.Error;
		}

		/// <summary>
		/// Initialize <see cref="LogListener"/>.
		/// </summary>
		protected LogListener()
		{
			Filters = new List<Func<LogMessage, bool>>();
		}

		/// <summary>
		/// The filter that only accepts messages of <see cref="LogLevels.Warning"/> type.
		/// </summary>
		public static readonly Func<LogMessage, bool> AllWarningFilter;

		/// <summary>
		/// The filter that only accepts messages of <see cref="LogLevels.Error"/> type.
		/// </summary>
		public static readonly Func<LogMessage, bool> AllErrorFilter;

		/// <summary>
		/// Messages filters that specify which messages should be handled.
		/// </summary>
		public IList<Func<LogMessage, bool>> Filters { get; private set; }

		private string _dateFormat = "yyyy/MM/dd";

		/// <summary>
		/// Date format. By default yyyy/MM/dd.
		/// </summary>
		public string DateFormat
		{
			get { return _dateFormat; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException("value");

				_dateFormat = value;
			}
		}

		private string _timeFormat = "HH:mm:ss.fff";

		/// <summary>
		/// Time format. By default HH:mm:ss.fff.
		/// </summary>
		public string TimeFormat
		{
			get { return _timeFormat; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException("value");
				
				_timeFormat = value;
			}
		}

		/// <summary>
		/// To record messages.
		/// </summary>
		/// <param name="messages">Debug messages.</param>
		public void WriteMessages(IEnumerable<LogMessage> messages)
		{
			if (Filters.Count > 0)
				messages = messages.Where(m => Filters.Any(f => f(m)));

			OnWriteMessages(messages);
		}

		/// <summary>
		/// To record messages.
		/// </summary>
		/// <param name="messages">Debug messages.</param>
		protected virtual void OnWriteMessages(IEnumerable<LogMessage> messages)
		{
			messages.ForEach(OnWriteMessage);
		}

		/// <summary>
		/// To record a message.
		/// </summary>
		/// <param name="message">A debug message.</param>
		protected virtual void OnWriteMessage(LogMessage message)
		{
			throw new NotSupportedException(LocalizedStrings.Str17);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Load(SettingsStorage storage)
		{
			DateFormat = storage.GetValue<string>("DateFormat");
			TimeFormat = storage.GetValue<string>("TimeFormat");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue("DateFormat", DateFormat);
			storage.SetValue("TimeFormat", TimeFormat);
		}
	}
}