namespace StockSharp.Algo.Candles
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// ����� ������.
	/// </summary>
	public class CandleSeries : Disposable, IPersistable, INotifyPropertyChanged
	{
		/// <summary>
		/// ������� <see cref="CandleSeries"/>.
		/// </summary>
		public CandleSeries()
		{
		}

		/// <summary>
		/// ������� <see cref="CandleSeries"/>.
		/// </summary>
		/// <param name="candleType">��� �����.</param>
		/// <param name="security">����������, �� �������� ���������� ����������� �����.</param>
		/// <param name="arg">�������� ������������ �����. ��������, ��� <see cref="TimeFrameCandle"/> ��� �������� <see cref="TimeFrameCandle.TimeFrame"/>.</param>
		public CandleSeries(Type candleType, Security security, object arg)
		{
			if (candleType == null)
				throw new ArgumentNullException("candleType");

			if (!candleType.IsSubclassOf(typeof(Candle)))
				throw new ArgumentOutOfRangeException("candleType", candleType, "������������ ��� ������.");

			if (security == null)
				throw new ArgumentNullException("security");

			if (arg == null)
				throw new ArgumentNullException("arg");

			_security = security;
			_candleType = candleType;
			_arg = arg;
			WorkingTime = security.CheckExchangeBoard().WorkingTime;
		}

		private Security _security;

		/// <summary>
		/// ����������, �� �������� ���������� ����������� �����.
		/// </summary>
		public virtual Security Security
		{
			get { return _security; }
			set
			{
				_security = value;
				RaisePropertyChanged("Security");
			}
		}

		private Type _candleType;

		/// <summary>
		/// ��� �����.
		/// </summary>
		public virtual Type CandleType
		{
			get { return _candleType; }
			set
			{
				_candleType = value;
				RaisePropertyChanged("CandleType");
			}
		}

		private object _arg;

		/// <summary>
		/// �������� ������������ �����. ��������, ��� <see cref="TimeFrameCandle"/> ��� �������� <see cref="TimeFrameCandle.TimeFrame"/>.
		/// </summary>
		public virtual object Arg
		{
			get { return _arg; }
			set
			{
				_arg = value;
				RaisePropertyChanged("Arg");
			}
		}

		/// <summary>
		/// ������� �������, � �������� ������� ������ ��������������� ����� ��� ������ �����.
		/// </summary>
		public WorkingTime WorkingTime { get; set; }

		private ICandleManager _candleManager;

		/// <summary>
		/// �������� ������, ������� ��������������� ������ �����.
		/// </summary>
		public ICandleManager CandleManager
		{
			get { return _candleManager; }
			set
			{
				if (value != _candleManager)
				{
					if (_candleManager != null)
					{
						_candleManager.Processing -= CandleManagerProcessing;
						_candleManager.Stopped -= CandleManagerStopped;
					}

					_candleManager = value;

					if (_candleManager != null)
					{
						_candleManager.Processing += CandleManagerProcessing;
						_candleManager.Stopped += CandleManagerStopped;
					}
				}
			}
		}

		/// <summary>
		/// ����������� ������ <see cref="Candle.VolumeProfileInfo"/>.
		/// ��-���������, ���������.
		/// </summary>
		public bool IsCalcVolumeProfile { get; set; }

		// ������������ RealTimeCandleBuilderSource
		internal bool IsNew { get; set; }

		/// <summary>
		/// ������� ��������� �����.
		/// </summary>
		public event Action<Candle> ProcessCandle;

		/// <summary>
		/// ������� ��������� ��������� �����.
		/// </summary>
		public event Action Stopped;

		private DateTimeOffset _from = DateTimeOffset.MinValue;

		/// <summary>
		/// ��������� ����, � ������� ���������� �������� ������.
		/// </summary>
		public DateTimeOffset From
		{
			get { return _from; }
			set { _from = value; }
		}

		private DateTimeOffset _to = DateTimeOffset.MaxValue;
		
		/// <summary>
		/// �������� ����, �� ������� ���������� �������� ������.
		/// </summary>
		public DateTimeOffset To
		{
			get { return _to; }
			set { _to = value; }
		}

		private void CandleManagerStopped(CandleSeries series)
		{
			if (series == this)
				Stopped.SafeInvoke();
		}

		private void CandleManagerProcessing(CandleSeries series, Candle candle)
		{
			if (series == this)
				ProcessCandle.SafeInvoke(candle);
		}

		/// <summary>
		/// ���������� ������� �������.
		/// </summary>
		protected override void DisposeManaged()
		{
			base.DisposeManaged();

			if (CandleManager != null)
				CandleManager.Stop(this);
		}

		/// <summary>
		/// �������� ��������� �������������.
		/// </summary>
		/// <returns>��������� �������������.</returns>
		public override string ToString()
		{
			return CandleType.Name + "_" + Security + "_" + TraderHelper.CandleArgToFolderName(Arg);
		}

		#region IPersistable

		/// <summary>
		/// ��������� ���������.
		/// </summary>
		/// <param name="storage">��������� ��������.</param>
		public void Load(SettingsStorage storage)
		{
			var connector = ConfigManager.TryGetService<IConnector>();
			if (connector != null)
			{
				var securityId = storage.GetValue<string>("SecurityId");

				if (!securityId.IsEmpty())
					Security = connector.LookupById(securityId);
			}

			CandleType = storage.GetValue<Type>("CandleType");
			Arg = storage.GetValue<object>("Arg");

			From = storage.GetValue<DateTimeOffset>("From");
			To = storage.GetValue<DateTimeOffset>("To");
			WorkingTime = storage.GetValue<WorkingTime>("WorkingTime");

			IsCalcVolumeProfile = storage.GetValue<bool>("IsCalcVolumeProfile");
		}

		/// <summary>
		/// ��������� ���������.
		/// </summary>
		/// <param name="storage">��������� ��������.</param>
		public void Save(SettingsStorage storage)
		{
			if (Security != null)
				storage.SetValue("SecurityId", Security.Id);

			storage.SetValue("CandleType", CandleType.GetTypeName(false));
			storage.SetValue("Arg", Arg);

			storage.SetValue("From", From);
			storage.SetValue("To", To);
			storage.SetValue("WorkingTime", WorkingTime);

			storage.SetValue("IsCalcVolumeProfile", IsCalcVolumeProfile);
		}

		#endregion

		#region INotifyPropertyChanged

		/// <summary>
		/// ������� ��������� ���������� �����.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// ������� ������� ��������� ���������� �����.
		/// </summary>
		protected void RaisePropertyChanged(string propertyName)
		{
			PropertyChanged.SafeInvoke(this, propertyName);
		}

		#endregion
	}
}