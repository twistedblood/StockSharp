namespace StockSharp.Algo.Candles
{
	using System;
	using System.Collections.Generic;

	using Ecng.ComponentModel;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// ������� �������� ������ (��������, ����������� <see cref="IConnector"/>, ��������������� ����������� ��������� ������� ������).
	/// </summary>
	public interface IExternalCandleSource
	{
		/// <summary>
		/// �������� ��������� ���������, ��� ������� � ������� ��������� ��� ������������ ����� ������ ���� ������.
		/// </summary>
		/// <param name="series">����� ������.</param>
		/// <returns>��������� ���������.</returns>
		IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series);

		/// <summary>
		/// ������� ��������� ����� ������, ���������� ����� �������� ����� <see cref="SubscribeCandles"/>.
		/// </summary>
		event Action<CandleSeries, IEnumerable<Candle>> NewCandles;

		/// <summary>
		/// ������� ��������� ��������� �����.
		/// </summary>
		event Action<CandleSeries> Stopped;

		/// <summary>
		/// ����������� �� ��������� ������.
		/// </summary>
		/// <param name="series">����� ������.</param>
		/// <param name="from">��������� ����, � ������� ���������� �������� ������.</param>
		/// <param name="to">�������� ����, �� ������� ���������� �������� ������.</param>
		void SubscribeCandles(CandleSeries series, DateTimeOffset from, DateTimeOffset to);

		/// <summary>
		/// ���������� �������� ��������� ������, ����� ��������� ����� <see cref="SubscribeCandles"/>.
		/// </summary>
		/// <param name="series">����� ������.</param>
		void UnSubscribeCandles(CandleSeries series);
	}
}