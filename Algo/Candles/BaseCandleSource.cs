namespace StockSharp.Algo.Candles
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.ComponentModel;

	/// <summary>
	/// ������� ���������� ���������� <see cref="ICandleSource{TValue}"/>.
	/// </summary>
	/// <typeparam name="TValue">��� ������.</typeparam>
	public abstract class BaseCandleSource<TValue> : Disposable, ICandleSource<TValue>
	{
		/// <summary>
		/// ���������������� <see cref="BaseCandleSource{TValue}"/>.
		/// </summary>
		protected BaseCandleSource()
		{
		}

		/// <summary>
		/// ��������� ��������� �� �������� (0 - ����� �����������).
		/// </summary>
		public abstract int SpeedPriority { get; }

		/// <summary>
		/// ������� ��������� ������ �������� ��� ���������.
		/// </summary>
		public event Action<CandleSeries, TValue> Processing;

		/// <summary>
		/// ������� ��������� ��������� �����.
		/// </summary>
		public event Action<CandleSeries> Stopped;

		/// <summary>
		/// ������� ������ �������������� ������.
		/// </summary>
		public event Action<Exception> Error;

		/// <summary>
		/// �������� ��������� ���������, ��� ������� � ������� ��������� ��� ������������ ����� ������ ���� ������.
		/// </summary>
		/// <param name="series">����� ������.</param>
		/// <returns>��������� ���������.</returns>
		public abstract IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series);

		/// <summary>
		/// ��������� ��������� ������.
		/// </summary>
		/// <param name="series">����� ������, ��� ������� ���������� ������ �������� ������.</param>
		/// <param name="from">��������� ����, � ������� ���������� �������� ������.</param>
		/// <param name="to">�������� ����, �� ������� ���������� �������� ������.</param>
		public abstract void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to);

		/// <summary>
		/// ���������� ��������� ������, ���������� ����� <see cref="Start"/>.
		/// </summary>
		/// <param name="series">����� ������.</param>
		public abstract void Stop(CandleSeries series);

		/// <summary>
		/// ������� ������� <see cref="Processing"/>.
		/// </summary>
		/// <param name="series">����� ������.</param>
		/// <param name="values">����� ������.</param>
		protected virtual void RaiseProcessing(CandleSeries series, TValue values)
		{
			Processing.SafeInvoke(series, values);
		}

		/// <summary>
		/// ������� ������� <see cref="Error"/>.
		/// </summary>
		/// <param name="error">�������� ������.</param>
		protected void RaiseError(Exception error)
		{
			Error.SafeInvoke(error);
		}

		/// <summary>
		/// ������� ������� <see cref="Stopped"/>.
		/// </summary>
		/// <param name="series">����� ������.</param>
		protected void RaiseStopped(CandleSeries series)
		{
			Stopped.SafeInvoke(series);
		}
	}
}