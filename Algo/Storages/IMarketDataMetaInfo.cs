namespace StockSharp.Algo.Storages
{
	using System;
	using System.IO;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// ����-���������� � ������ �� ���� ����.
	/// </summary>
	public interface IMarketDataMetaInfo
	{
		/// <summary>
		/// ���� ���.
		/// </summary>
		DateTime Date { get; }

		/// <summary>
		/// ���������� ������.
		/// </summary>
		int Count { get; set; }

		/// <summary>
		/// �������� <see cref="Security.PriceStep"/> � ���� <see cref="Date"/>.
		/// </summary>
		decimal PriceStep { get; set; }

		/// <summary>
		/// �������� <see cref="Security.VolumeStep"/> � ���� <see cref="Date"/>.
		/// </summary>
		decimal VolumeStep { get; set; }

		/// <summary>
		/// ����� ������ ������.
		/// </summary>
		DateTime FirstTime { get; set; }

		/// <summary>
		/// ����� ��������� ������.
		/// </summary>
		DateTime LastTime { get; set; }

		/// <summary>
		/// ������������� ��������� ������.
		/// </summary>
		object LastId { get; }

		/// <summary>
		/// ��������� ��������� ����-���������� � �����.
		/// </summary>
		/// <param name="stream">����� ������.</param>
		void Write(Stream stream);

		/// <summary>
		/// ��������� ��������� ����-���������� �� ������.
		/// </summary>
		/// <param name="stream">����� ������.</param>
		void Read(Stream stream);
	}

	abstract class MetaInfo : IMarketDataMetaInfo
	{
		protected MetaInfo(DateTime date)
		{
			Date = date;
		}

		public DateTime Date { get; private set; }
		public int Count { get; set; }

		public decimal PriceStep { get; set; }
		public decimal VolumeStep { get; set; }

		public DateTime FirstTime { get; set; }
		public DateTime LastTime { get; set; }

		public abstract object LastId { get; }

		/// <summary>
		/// ��������� ��������� ����-���������� � �����.
		/// </summary>
		/// <param name="stream">����� ������.</param>
		public abstract void Write(Stream stream);

		/// <summary>
		/// ��������� ��������� ����-���������� �� ������.
		/// </summary>
		/// <param name="stream">����� ������.</param>
		public abstract void Read(Stream stream);

		//public static TMetaInfo CreateMetaInfo(DateTime date)
		//{
		//	return typeof(TMetaInfo).CreateInstance<TMetaInfo>(date);
		//}
	}
}