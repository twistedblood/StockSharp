namespace StockSharp.Algo.Statistics
{
	using System;

	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Интерфейс, описывающий параметр статистики, рассчитывающийся на основе значение прибыли-убытка (максимальная просадка, коэффициент Шарпа и т.д.).
	/// </summary>
	public interface IPnLStatisticParameter
	{
		/// <summary>
		/// Добавить в параметр новые данные.
		/// </summary>
		/// <param name="marketTime">Биржевое время.</param>
		/// <param name="pnl">Значение прибыли убытка.</param>
		void Add(DateTimeOffset marketTime, decimal pnl);
	}

	/// <summary>
	/// Максимальная значение прибыли за весь период.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str958Key)]
	[DescriptionLoc(LocalizedStrings.Str959Key)]
	[CategoryLoc(LocalizedStrings.PnLKey)]
	public class MaxProfitParameter : BaseStatisticParameter<decimal>, IPnLStatisticParameter
	{
		/// <summary>
		/// Добавить в параметр новые данные.
		/// </summary>
		/// <param name="marketTime">Биржевое время.</param>
		/// <param name="pnl">Значение прибыли убытка.</param>
		public void Add(DateTimeOffset marketTime, decimal pnl)
		{
			Value = Math.Max(Value, pnl);
		}
	}

	/// <summary>
	/// Максимальная абсолютная просадка за весь период.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str960Key)]
	[DescriptionLoc(LocalizedStrings.Str961Key)]
	[CategoryLoc(LocalizedStrings.PnLKey)]
	public class MaxDrawdownParameter : BaseStatisticParameter<decimal>, IPnLStatisticParameter
	{
		private decimal _maxEquity = decimal.MinValue;

		/// <summary>
		/// Добавить в параметр новые данные.
		/// </summary>
		/// <param name="marketTime">Биржевое время.</param>
		/// <param name="pnl">Значение прибыли убытка.</param>
		public void Add(DateTimeOffset marketTime, decimal pnl)
		{
			_maxEquity = Math.Max(_maxEquity, pnl);
			Value = Math.Max(Value, _maxEquity - pnl);
		}

		/// <summary>
		/// Сохранить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("MaxEquity", _maxEquity);
			base.Save(storage);
		}

		/// <summary>
		/// Загрузить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			_maxEquity = storage.GetValue<decimal>("MaxEquity");
			base.Load(storage);
		}
	}

	/// <summary>
	/// Максимальная относительная просадка эквити за весь период.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str962Key)]
	[DescriptionLoc(LocalizedStrings.Str963Key)]
	[CategoryLoc(LocalizedStrings.PnLKey)]
	public class MaxRelativeDrawdownParameter : BaseStatisticParameter<decimal>, IPnLStatisticParameter
	{
		private decimal _maxEquity = decimal.MinValue;

		/// <summary>
		/// Добавить в параметр новые данные.
		/// </summary>
		/// <param name="marketTime">Биржевое время.</param>
		/// <param name="pnl">Значение прибыли убытка.</param>
		public void Add(DateTimeOffset marketTime, decimal pnl)
		{
			_maxEquity = Math.Max(_maxEquity, pnl);

			var drawdown = _maxEquity - pnl;
			Value = Math.Max(Value, _maxEquity != 0 ? drawdown / _maxEquity : 0);
		}

		/// <summary>
		/// Сохранить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("MaxEquity", _maxEquity);
			base.Save(storage);
		}

		/// <summary>
		/// Загрузить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			_maxEquity = storage.GetValue<decimal>("MaxEquity");
			base.Load(storage);
		}
	}

	/// <summary>
	/// Относительная прибыль за весь отрезок времени.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str964Key)]
	[DescriptionLoc(LocalizedStrings.Str965Key)]
	[CategoryLoc(LocalizedStrings.PnLKey)]
	public class ReturnParameter : BaseStatisticParameter<decimal>, IPnLStatisticParameter
	{
		private decimal _minEquity = decimal.MaxValue;

		/// <summary>
		/// Добавить в параметр новые данные.
		/// </summary>
		/// <param name="marketTime">Биржевое время.</param>
		/// <param name="pnl">Значение прибыли убытка.</param>
		public void Add(DateTimeOffset marketTime, decimal pnl)
		{
			_minEquity = Math.Min(_minEquity, pnl);

			var profit = pnl - _minEquity;
			Value = Math.Max(Value, _minEquity != 0 ? profit / _minEquity : 0);
		}

		/// <summary>
		/// Сохранить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("MinEquity", _minEquity);
			base.Save(storage);
		}

		/// <summary>
		/// Загрузить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			_minEquity = storage.GetValue<decimal>("MinEquity");
			base.Load(storage);
		}
	}

	/// <summary>
	/// Коэффициент восстановления (чистая прибыль / максимальная просадка).
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str966Key)]
	[DescriptionLoc(LocalizedStrings.Str967Key)]
	[CategoryLoc(LocalizedStrings.PnLKey)]
	public class RecoveryFactorParameter : BaseStatisticParameter<decimal>, IPnLStatisticParameter
	{
		private readonly MaxDrawdownParameter _maxDrawdown = new MaxDrawdownParameter();
		private readonly NetProfitParameter _netProfit = new NetProfitParameter();

		/// <summary>
		/// Добавить в параметр новые данные.
		/// </summary>
		/// <param name="marketTime">Биржевое время.</param>
		/// <param name="pnl">Значение прибыли убытка.</param>
		public void Add(DateTimeOffset marketTime, decimal pnl)
		{
			_maxDrawdown.Add(marketTime, pnl);
			_netProfit.Add(marketTime, pnl);

			Value = _maxDrawdown.Value != 0 ? _netProfit.Value / _maxDrawdown.Value : 0;
		}

		/// <summary>
		/// Сохранить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("MaxDrawdown", _maxDrawdown.Save());
			storage.SetValue("NetProfit", _netProfit.Save());

			base.Save(storage);
		}

		/// <summary>
		/// Загрузить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			_maxDrawdown.Load(storage.GetValue<SettingsStorage>("MaxDrawdown"));
			_netProfit.Load(storage.GetValue<SettingsStorage>("NetProfit"));

			base.Load(storage);
		}
	}

	/// <summary>
	/// Чистая прибыль за весь отрезок времени.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str968Key)]
	[DescriptionLoc(LocalizedStrings.Str969Key)]
	[CategoryLoc(LocalizedStrings.PnLKey)]
	public class NetProfitParameter : BaseStatisticParameter<decimal>, IPnLStatisticParameter
	{
		private decimal? _firstPnL;

		/// <summary>
		/// Добавить в параметр новые данные.
		/// </summary>
		/// <param name="marketTime">Биржевое время.</param>
		/// <param name="pnl">Значение прибыли убытка.</param>
		public void Add(DateTimeOffset marketTime, decimal pnl)
		{
			if (_firstPnL == null)
				_firstPnL = pnl;

			Value = pnl - _firstPnL.Value;
		}

		/// <summary>
		/// Сохранить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("FirstPnL", _firstPnL);
			base.Save(storage);
		}

		/// <summary>
		/// Загрузить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			_firstPnL = storage.GetValue<decimal?>("FirstPnL");
			base.Load(storage);
		}
	}
}