namespace StockSharp.Algo.Candles.Compression
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.ComponentModel;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// ������� �������� ������ ��� <see cref="ICandleBuilder"/>, ������� �������� ������ �� <see cref="IConnector"/>.
	/// </summary>
	/// <typeparam name="T">��� �������� ������ (��������, <see cref="Trade"/>).</typeparam>
	public abstract class RealTimeCandleBuilderSource<T> : ConvertableCandleBuilderSource<T>
	{
		private readonly SynchronizedDictionary<Security, CachedSynchronizedList<CandleSeries>> _registeredSeries = new SynchronizedDictionary<Security, CachedSynchronizedList<CandleSeries>>();

		/// <summary>
		/// ������� <see cref="RealTimeCandleBuilderSource{T}"/>.
		/// </summary>
		/// <param name="connector">�����������, ����� ������� ����� ���������� ����� ������.</param>
		protected RealTimeCandleBuilderSource(IConnector connector)
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			Connector = connector;
		}

		/// <summary>
		/// ��������� ��������� �� �������� (0 - ����� �����������).
		/// </summary>
		public override int SpeedPriority
		{
			get { return 1; }
		}

		/// <summary>
		/// �����������, ����� ������� ����� ���������� ����� ������.
		/// </summary>
		public IConnector Connector { get; private set; }

		/// <summary>
		/// ��������� ��������� ������.
		/// </summary>
		/// <param name="series">����� ������, ��� ������� ���������� ������ �������� ������.</param>
		/// <param name="from">��������� ����, � ������� ���������� �������� ������.</param>
		/// <param name="to">�������� ����, �� ������� ���������� �������� ������.</param>
		public override void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			bool registerSecurity;

			series.IsNew = true;
			_registeredSeries.SafeAdd(series.Security, out registerSecurity).Add(series);

			if (registerSecurity)
				RegisterSecurity(series.Security);
		}

		/// <summary>
		/// ���������� ��������� ������, ���������� ����� <see cref="Start"/>.
		/// </summary>
		/// <param name="series">����� ������.</param>
		public override void Stop(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			var registeredSeries = _registeredSeries.TryGetValue(series.Security);

			if (registeredSeries == null)
				return;

			registeredSeries.Remove(series);

			if (registeredSeries.Count == 0)
			{
				UnRegisterSecurity(series.Security);
				_registeredSeries.Remove(series.Security);
			}

			RaiseStopped(series);
		}

		/// <summary>
		/// ���������������� ��������� ������ ��� �����������.
		/// </summary>
		/// <param name="security">����������.</param>
		protected abstract void RegisterSecurity(Security security);

		/// <summary>
		/// ���������� ��������� ������ ��� �����������.
		/// </summary>
		/// <param name="security">����������.</param>
		protected abstract void UnRegisterSecurity(Security security);

		/// <summary>
		/// �������� ����� ����������� ��������.
		/// </summary>
		/// <param name="security">����������.</param>
		/// <returns>����������� ��������.</returns>
		protected abstract IEnumerable<T> GetSecurityValues(Security security);

		/// <summary>
		/// �������� ��������� ����� ������, ���������� �� <see cref="Connector"/>.
		/// </summary>
		/// <param name="values">����� ������.</param>
		protected void AddNewValues(IEnumerable<T> values)
		{
			if (_registeredSeries.Count == 0)
				return;

			foreach (var group in Convert(values).GroupBy(v => v.Security))
			{
				var security = group.Key;

				var registeredSeries = _registeredSeries.TryGetValue(security);

				if (registeredSeries == null)
					continue;

				var seriesCache = registeredSeries.Cache;

				var securityValues = group.OrderBy(v => v.Id).ToArray();

				foreach (var series in seriesCache)
				{
					if (series.IsNew)
					{
						RaiseProcessing(series, Convert(GetSecurityValues(security)).OrderBy(v => v.Id));
						series.IsNew = false;
					}
					else
					{
						RaiseProcessing(series, securityValues);
					}
				}
			}
		}
	}

	/// <summary>
	/// �������� ������ ��� <see cref="CandleBuilder{TCandle}"/>, ������� ������� <see cref="ICandleBuilderSourceValue"/> �� ������� ������ <see cref="Trade"/>.
	/// </summary>
	public class TradeCandleBuilderSource : RealTimeCandleBuilderSource<Trade>
	{
		/// <summary>
		/// ������� <see cref="TradeCandleBuilderSource"/>.
		/// </summary>
		/// <param name="connector">�����������, ����� ������� ����� ���������� ����� ������, ��������� ������� <see cref="IConnector.NewTrades"/>.</param>
		public TradeCandleBuilderSource(IConnector connector)
			: base(connector)
		{
			Connector.NewTrades += AddNewValues;
		}

		/// <summary>
		/// �������� ��������� ���������, ��� ������� � ������� ��������� ��� ������������ ����� ������ ���� ������.
		/// </summary>
		/// <param name="series">����� ������.</param>
		/// <returns>��������� ���������.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			var trades = GetSecurityValues(series.Security);

			yield return new Range<DateTimeOffset>(trades.IsEmpty() ? Connector.CurrentTime : trades.Min(v => v.Time), DateTimeOffset.MaxValue);
		}

		/// <summary>
		/// ���������������� ��������� ������ ��� �����������.
		/// </summary>
		/// <param name="security">����������.</param>
		protected override void RegisterSecurity(Security security)
		{
			Connector.RegisterTrades(security);
		}

		/// <summary>
		/// ���������� ��������� ������ ��� �����������.
		/// </summary>
		/// <param name="security">����������.</param>
		protected override void UnRegisterSecurity(Security security)
		{
			Connector.UnRegisterTrades(security);
		}

		/// <summary>
		/// �������� ����� ����������� ��������.
		/// </summary>
		/// <param name="security">����������.</param>
		/// <returns>����������� ��������.</returns>
		protected override IEnumerable<Trade> GetSecurityValues(Security security)
		{
			return Connector.Trades.Filter(security);
		}

		/// <summary>
		/// ���������� ������� �������.
		/// </summary>
		protected override void DisposeManaged()
		{
			Connector.NewTrades -= AddNewValues;
			base.DisposeManaged();
		}
	}

	/// <summary>
	/// �������� ������ ��� <see cref="CandleBuilder{TCandle}"/>, ������� ������� <see cref="ICandleBuilderSourceValue"/> �� ������� <see cref="MarketDepth"/>.
	/// </summary>
	public class MarketDepthCandleBuilderSource : RealTimeCandleBuilderSource<MarketDepth>
	{
		/// <summary>
		/// ������� <see cref="MarketDepthCandleBuilderSource"/>.
		/// </summary>
		/// <param name="connector">�����������, ����� ������� ����� ���������� ���������� �������, ��������� ������� <see cref="IConnector.MarketDepthsChanged"/>.</param>
		public MarketDepthCandleBuilderSource(IConnector connector)
			: base(connector)
		{
			Connector.MarketDepthsChanged += OnMarketDepthsChanged;
		}

		/// <summary>
		/// �������� ��������� ���������, ��� ������� � ������� ��������� ��� ������������ ����� ������ ���� ������.
		/// </summary>
		/// <param name="series">����� ������.</param>
		/// <returns>��������� ���������.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			yield return new Range<DateTimeOffset>(Connector.CurrentTime, DateTimeOffset.MaxValue);
		}

		/// <summary>
		/// ���������������� ��������� ������ ��� �����������.
		/// </summary>
		/// <param name="security">����������.</param>
		protected override void RegisterSecurity(Security security)
		{
			Connector.RegisterMarketDepth(security);
		}

		/// <summary>
		/// ���������� ��������� ������ ��� �����������.
		/// </summary>
		/// <param name="security">����������.</param>
		protected override void UnRegisterSecurity(Security security)
		{
			Connector.UnRegisterMarketDepth(security);
		}

		/// <summary>
		/// �������� ����� ����������� ��������.
		/// </summary>
		/// <param name="security">����������.</param>
		/// <returns>����������� ��������.</returns>
		protected override IEnumerable<MarketDepth> GetSecurityValues(Security security)
		{
			return Enumerable.Empty<MarketDepth>();
		}

		private void OnMarketDepthsChanged(IEnumerable<MarketDepth> depths)
		{
			AddNewValues(depths.Select(d => d.Clone()));
		}

		/// <summary>
		/// ���������� ������� �������.
		/// </summary>
		protected override void DisposeManaged()
		{
			Connector.MarketDepthsChanged -= OnMarketDepthsChanged;
			base.DisposeManaged();
		}
	}
}