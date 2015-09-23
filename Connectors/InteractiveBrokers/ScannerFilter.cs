namespace StockSharp.InteractiveBrokers
{
	using System;

	/// <summary>
	/// ���� ���������� �����.
	/// </summary>
	public enum ScannerFilterStockExcludes
	{
		/// <summary>
		/// �� ��������� ������.
		/// </summary>
		All,

		/// <summary>
		/// ��������� <see cref="Etf"/>.
		/// </summary>
		Stock,

		/// <summary>
		/// ������ Exchange-traded fund.
		/// </summary>
		Etf
	}

	/// <summary>
	/// ��������� ������� �������, ������������ ����� <see cref="IBTrader.SubscribeScanner"/>.
	/// </summary>
	public class ScannerFilter
	{
		/// <summary>
		/// ������� <see cref="ScannerFilter"/>.
		/// </summary>
		public ScannerFilter()
		{
		}

		/// <summary>
		/// ���������� ����� � �������.
		/// </summary>
		public int? RowCount { get; set; }

		/// <summary>
		/// ��� �����������.
		/// </summary>
		public string SecurityType { get; set; }

		/// <summary>
		/// �������� ��������.
		/// </summary>
		public string BoardCode { get; set; }

		/// <summary>
		///  
		/// </summary>
		public string ScanCode { get; set; }

		/// <summary>
		/// ������� ������ �������� ���� �����������.
		/// </summary>
		public decimal? AbovePrice { get; set; }

		/// <summary>
		/// ������ ������ �������� ���� �����������.
		/// </summary>
		public decimal? BelowPrice { get; set; }

		/// <summary>
		/// ������� ������ ������ ������ �� �����������.
		/// </summary>
		public int? AboveVolume { get; set; }

		/// <summary>
		/// ������� ������ ������ ������ �� �������.
		/// </summary>
		public int? AverageOptionVolumeAbove { get; set; }

		/// <summary>
		/// ������� ������ �������������.
		/// </summary>
		public decimal? MarketCapAbove { get; set; }

		/// <summary>
		/// ������ ������ �������������.
		/// </summary>
		public decimal? MarketCapBelow { get; set; }

		/// <summary>
		/// ������� ������ �������� Moody.
		/// </summary>
		public string MoodyRatingAbove { get; set; }

		/// <summary>
		/// ������ ������ �������� Moody.
		/// </summary>
		public string MoodyRatingBelow { get; set; }

		/// <summary>
		/// ������� ������ �������� SP.
		/// </summary>
		public string SpRatingAbove { get; set; }

		/// <summary>
		/// ������ ������ �������� SP.
		/// </summary>
		public string SpRatingBelow { get; set; }

		/// <summary>
		/// ������� ������ ���� ��������� �����������.
		/// </summary>
		public DateTimeOffset? MaturityDateAbove { get; set; }

		/// <summary>
		/// ������ ������ ���� ��������� �����������.
		/// </summary>
		public DateTimeOffset? MaturityDateBelow { get; set; }

		/// <summary>
		/// ������� ������ �������� ������.
		/// </summary>
		public decimal? CouponRateAbove { get; set; }

		/// <summary>
		/// ������ ������ �������� ������.
		/// </summary>
		public decimal? CouponRateBelow { get; set; }

		/// <summary>
		/// ��������� �������������� ���������.
		/// </summary>
		public bool ExcludeConvertibleBonds { get; set; }

		/// <summary>
		/// ����������� ���������. ���������, http://www.interactivebrokers.com/en/software/tws/usersguidebook/technicalanalytics/market_scanner_types.htm .
		/// </summary>
		public string ScannerSettingPairs { get; set; }

		/// <summary>
		/// ��� ���������� �����.
		/// </summary>
		public ScannerFilterStockExcludes StockTypeExclude { get; set; }
	}
}