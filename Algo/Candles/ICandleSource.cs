namespace StockSharp.Algo.Candles
{
	using System;
	using System.Collections.Generic;

	using Ecng.ComponentModel;

	/// <summary>
	/// �������� ������.
	/// </summary>
	/// <typeparam name="TValue">��� ������.</typeparam>
	public interface ICandleSource<TValue> : IDisposable
	{
		/// <summary>
		/// ��������� ��������� �� �������� (0 - ����� �����������).
		/// </summary>
		int SpeedPriority { get; }

		/// <summary>
		/// ������� ��������� ������ �������� ��� ���������.
		/// </summary>
		event Action<CandleSeries, TValue> Processing;

		/// <summary>
		/// ������� ��������� ��������� �����.
		/// </summary>
		event Action<CandleSeries> Stopped;

		/// <summary>
		/// ������� ������ �������������� ������.
		/// </summary>
		event Action<Exception> Error;

		/// <summary>
		/// �������� ��������� ���������, ��� ������� � ������� ��������� ��� ������������ ����� ������ ���� ������.
		/// </summary>
		/// <param name="series">����� ������.</param>
		/// <returns>��������� ���������.</returns>
		IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series);

		/// <summary>
		/// ��������� ��������� ������.
		/// </summary>
		/// <param name="series">����� ������, ��� ������� ���������� ������ �������� ������.</param>
		/// <param name="from">��������� ����, � ������� ���������� �������� ������.</param>
		/// <param name="to">�������� ����, �� ������� ���������� �������� ������.</param>
		void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to);

		/// <summary>
		/// ���������� ��������� ������, ���������� ����� <see cref="Start"/>.
		/// </summary>
		/// <param name="series">����� ������.</param>
		void Stop(CandleSeries series);
	}
}