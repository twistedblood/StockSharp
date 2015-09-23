namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.IO;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// ���������, ����������� ���������, ��������������� � <see cref="IMarketDataStorage"/>.
	/// </summary>
	public interface IMarketDataStorageDrive
	{
		/// <summary>
		/// ��������� (���� ������, ���� � �.�.).
		/// </summary>
		IMarketDataDrive Drive { get; }

		/// <summary>
		/// �������� ��� ����, ��� ������� �������� ������-������.
		/// </summary>
		IEnumerable<DateTime> Dates { get; }

		/// <summary>
		/// ������� ���-�����, �������� � ���� ���������� � ��������� ���������� �������.
		/// </summary>
		void ClearDatesCache();

		/// <summary>
		/// ������� ������-������ �� ��������� �� ��������� ����.
		/// </summary>
		/// <param name="date">����, ��� ������� ���������� ������� ��� ������.</param>
		void Delete(DateTime date);

		/// <summary>
		/// ��������� ������ � ������� ��������� StockSharp.
		/// </summary>
		/// <param name="date">����, ��� ������� ���������� ��������� ������.</param>
		/// <param name="stream">������ � ������� ��������� StockSharp.</param>
		void SaveStream(DateTime date, Stream stream);

		/// <summary>
		/// ��������� ������ � ������� ��������� StockSharp.
		/// </summary>
		/// <param name="date">����, ��� ������� ���������� ��������� ������.</param>
		/// <returns>������ � ������� ��������� StockSharp. ���� ������ �� ����������, �� ����� ���������� <see cref="Stream.Null"/>.</returns>
		Stream LoadStream(DateTime date);
	}

	/// <summary>
	/// ���������, ����������� ��������� (���� ������, ���� � �.�.).
	/// </summary>
	public interface IMarketDataDrive : IPersistable, IDisposable
	{
		/// <summary>
		/// ���� � �������.
		/// </summary>
		string Path { get; }

		/// <summary>
		/// �������� ��������� ��������.
		/// </summary>
		/// <param name="serializer">������������.</param>
		/// <returns>��������� ��������.</returns>
		IMarketDataStorage<NewsMessage> GetNewsMessageStorage(IMarketDataSerializer<NewsMessage> serializer);

		/// <summary>
		/// �������� ��������� ��� �����������.
		/// </summary>
		/// <param name="security">����������.</param>
		/// <returns>��������� ��� �����������.</returns>
		ISecurityMarketDataDrive GetSecurityDrive(Security security);

		/// <summary>
		/// �������� ��� ����������� ��������� ���� ������ � �����������.
		/// </summary>
		/// <param name="securityId">������������� �����������.</param>
		/// <param name="format">��� �������.</param>
		/// <returns>��������� ���� ������ � �����������.</returns>
		IEnumerable<Tuple<Type, object[]>> GetCandleTypes(SecurityId securityId, StorageFormats format);

		/// <summary>
		/// �������� ��������� ��� <see cref="IMarketDataStorage"/>.
		/// </summary>
		/// <param name="securityId">������������� �����������.</param>
		/// <param name="dataType">��� ������-������.</param>
		/// <param name="arg">��������, ��������������� � ����� <paramref name="dataType"/>. ��������, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="format">��� �������.</param>
		/// <returns>��������� ��� <see cref="IMarketDataStorage"/>.</returns>
		IMarketDataStorageDrive GetStorageDrive(SecurityId securityId, Type dataType, object arg, StorageFormats format);
	}

	/// <summary>
	/// ������� ���������� <see cref="IMarketDataDrive"/>.
	/// </summary>
	public abstract class BaseMarketDataDrive : Disposable, IMarketDataDrive
	{
		/// <summary>
		/// ���������������� <see cref="BaseMarketDataDrive"/>.
		/// </summary>
		protected BaseMarketDataDrive()
		{
		}

		/// <summary>
		/// ���� � �������.
		/// </summary>
		public abstract string Path { get; set; }

		/// <summary>
		/// �������� ��������� ��������.
		/// </summary>
		/// <param name="serializer">������������.</param>
		/// <returns>��������� ��������.</returns>
		public IMarketDataStorage<NewsMessage> GetNewsMessageStorage(IMarketDataSerializer<NewsMessage> serializer)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// �������� ��������� ��� �����������.
		/// </summary>
		/// <param name="security">����������.</param>
		/// <returns>��������� ��� �����������.</returns>
		public ISecurityMarketDataDrive GetSecurityDrive(Security security)
		{
			return new SecurityMarketDataDrive(this, security);
		}

		/// <summary>
		/// �������� ��� ����������� ��������� ���� ������ � �����������.
		/// </summary>
		/// <param name="securityId">������������� �����������.</param>
		/// <param name="format">��� �������.</param>
		/// <returns>��������� ���� ������ � �����������.</returns>
		public abstract IEnumerable<Tuple<Type, object[]>> GetCandleTypes(SecurityId securityId, StorageFormats format);

		/// <summary>
		/// ������� ��������� ��� <see cref="IMarketDataStorage"/>.
		/// </summary>
		/// <param name="securityId">������������� �����������.</param>
		/// <param name="dataType">��� ������-������.</param>
		/// <param name="arg">��������, ��������������� � ����� <paramref name="dataType"/>. ��������, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="format">��� �������.</param>
		/// <returns>��������� ��� <see cref="IMarketDataStorage"/>.</returns>
		public abstract IMarketDataStorageDrive GetStorageDrive(SecurityId securityId, Type dataType, object arg, StorageFormats format);

		/// <summary>
		/// ��������� ���������.
		/// </summary>
		/// <param name="storage">��������� ��������.</param>
		public virtual void Load(SettingsStorage storage)
		{
			Path = storage.GetValue<string>("Path");
		}

		/// <summary>
		/// ��������� ���������.
		/// </summary>
		/// <param name="storage">��������� ��������.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue("Path", Path);
		}

		/// <summary>
		/// �������� ��������� �������������.
		/// </summary>
		/// <returns>��������� �������������.</returns>
		public override string ToString()
		{
			return Path;
		}
	}
}