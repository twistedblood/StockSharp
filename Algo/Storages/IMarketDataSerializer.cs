namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;

	using Ecng.Collections;

	/// <summary>
	/// ������������.
	/// </summary>
	public interface IMarketDataSerializer
	{
		/// <summary>
		/// ������� ������ ����-����������.
		/// </summary>
		/// <param name="date">����.</param>
		/// <returns>����-���������� � ������ �� ���� ����.</returns>
		IMarketDataMetaInfo CreateMetaInfo(DateTime date);

		/// <summary>
		/// ������������� ������ � ����� ������.
		/// </summary>
		/// <param name="stream">����� ������.</param>
		/// <param name="data">������.</param>
		/// <param name="metaInfo">����-���������� � ������ �� ���� ����.</param>
		void Serialize(Stream stream, IEnumerable data, IMarketDataMetaInfo metaInfo);

		/// <summary>
		/// ��������� ������ �� ������.
		/// </summary>
		/// <param name="stream">����� ������.</param>
		/// <param name="metaInfo">����-���������� � ������ �� ���� ����.</param>
		/// <returns>������.</returns>
		IEnumerableEx Deserialize(Stream stream, IMarketDataMetaInfo metaInfo);
	}

	/// <summary>
	/// ������������.
	/// </summary>
	/// <typeparam name="TData">��� ������.</typeparam>
	public interface IMarketDataSerializer<TData> : IMarketDataSerializer
	{
		/// <summary>
		/// ������������� ������ � ����� ������.
		/// </summary>
		/// <param name="stream">����� ������.</param>
		/// <param name="data">������.</param>
		/// <param name="metaInfo">����-���������� � ������ �� ���� ����.</param>
		void Serialize(Stream stream, IEnumerable<TData> data, IMarketDataMetaInfo metaInfo);

		/// <summary>
		/// ��������� ������ �� ������.
		/// </summary>
		/// <param name="stream">�����.</param>
		/// <param name="metaInfo">����-���������� � ������ �� ���� ����.</param>
		/// <returns>������.</returns>
		new IEnumerableEx<TData> Deserialize(Stream stream, IMarketDataMetaInfo metaInfo);
	}
}