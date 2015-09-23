namespace StockSharp.SmartCom
{
	using System.Net;

	using Ecng.Common;

	/// <summary>
	/// ������ �������� ������� SmartCOM. �������� �������� �� ������ http://www.itinvest.ru/software/trade-servers/ .
	/// </summary>
	public static class SmartComAddresses
	{
		///// <summary>
		///// ���� ������� �� ���������, ������ 8090.
		///// </summary>
		//public const int DefaultPort = 8090;

		///// <summary>
		///// �������� ������. IP ����� 82.204.220.34, ���� 8090.
		///// </summary>
		//public static readonly EndPoint Major = "82.204.220.34:8090".To<EndPoint>();

		///// <summary>
		///// ��������������� ������. IP ����� 213.247.232.238, ���� 8090.
		///// </summary>
		//public static readonly EndPoint Minor = "213.247.232.238:8090".To<EndPoint>();

		///// <summary>
		///// ��������� ������. IP ����� 87.118.223.109, ���� 8090.
		///// </summary>
		//public static readonly EndPoint Reserv = "87.118.223.109:8090".To<EndPoint>();

		///// <summary>
		///// C����� ��������. IP ����� 89.175.35.230, ���� 8090.
		///// </summary>
		//public static readonly EndPoint Stalker = "89.175.35.230:8090".To<EndPoint>();

		/// <summary>
		/// ���� ������. IP ����� mxdemo.ittrade.ru, ���� 8443.
		/// </summary>
		public static readonly EndPoint Demo = "mxdemo.ittrade.ru:8443".To<EndPoint>();

		/// <summary>
		/// MatriX� ������. IP ����� mx.ittrade.ru, ���� 8443.
		/// </summary>
		public static readonly EndPoint Matrix = "mx.ittrade.ru:8443".To<EndPoint>();

		/// <summary>
		/// ��������� ������. IP ����� st1.ittrade.ru, ���� 8090.
		/// </summary>
		public static readonly EndPoint Reserve1 = "st1.ittrade.ru:8090".To<EndPoint>();

		/// <summary>
		/// ��������� ������. IP ����� st2.ittrade.ru, ���� 8090.
		/// </summary>
		public static readonly EndPoint Reserve2 = "st2.ittrade.ru:8090".To<EndPoint>();
	}
}