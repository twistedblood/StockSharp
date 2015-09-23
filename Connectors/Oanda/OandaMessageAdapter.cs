namespace StockSharp.Oanda
{
	using System;

	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Messages;
	using StockSharp.Oanda.Native;

	/// <summary>
	/// The messages adapter for OANDA (REST protocol).
	/// </summary>
	public partial class OandaMessageAdapter : MessageAdapter
	{
		private OandaRestClient _restClient;
		private OandaStreamingClient _streamigClient;

		/// <summary>
		/// Initializes a new instance of the <see cref="OandaMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public OandaMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			HeartbeatInterval = TimeSpan.FromSeconds(60);

			this.AddMarketDataSupport();
			this.AddTransactionalSupport();
		}

		/// <summary>
		/// Create condition for order type <see cref="OrderTypes.Conditional"/>, that supports the adapter.
		/// </summary>
		/// <returns>Order condition. If the connection does not support the order type <see cref="OrderTypes.Conditional"/>, it will be returned <see langword="null" />.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new OandaOrderCondition();
		}

		/// <summary>
		/// Gets a value indicating whether the connector supports security lookup.
		/// </summary>
		protected override bool IsSupportNativeSecurityLookup
		{
			get { return true; }
		}

		/// <summary>
		/// Gets a value indicating whether the connector supports position lookup.
		/// </summary>
		protected override bool IsSupportNativePortfolioLookup
		{
			get { return true; }
		}

		private void StreamingClientDispose()
		{
			_streamigClient.NewError -= SendOutError;
			_streamigClient.NewTransaction -= SessionOnNewTransaction;
			_streamigClient.NewPrice -= SessionOnNewPrice;

			_streamigClient.Dispose();
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_accountIds.Clear();

					if (_streamigClient != null)
					{
						try
						{
							StreamingClientDispose();
						}
						catch (Exception ex)
						{
							SendOutError(ex);
						}

						_streamigClient = null;
					}

					_restClient = null;

					SendOutMessage(new ResetMessage());

					break;
				}

				case MessageTypes.Connect:
				{
					if (_restClient != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					if (_streamigClient != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					_restClient = new OandaRestClient(Server, Token);
					
					_streamigClient = new OandaStreamingClient(Server, Token, GetAccountId);
					_streamigClient.NewError += SendOutError;
					_streamigClient.NewTransaction += SessionOnNewTransaction;
					_streamigClient.NewPrice += SessionOnNewPrice;

					SendOutMessage(new ConnectMessage());

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_restClient == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					if (_streamigClient == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					StreamingClientDispose();
					_streamigClient = null;

					_restClient = null;

					SendOutMessage(new DisconnectMessage());

					break;
				}

				case MessageTypes.PortfolioLookup:
				{
					ProcessPortfolioLookupMessage((PortfolioLookupMessage)message);
					break;
				}

				case MessageTypes.Portfolio:
				{
					ProcessPortfolioMessage((PortfolioMessage)message);
					break;
				}

				case MessageTypes.OrderStatus:
				{
					ProcessOrderStatusMessage();
					break;
				}

				case MessageTypes.Time:
				{
					//var timeMsg = (TimeMessage)message;
					//Session.RequestHeartbeat(new HeartbeatRequest(timeMsg.TransactionId), () => { }, CreateErrorHandler("RequestHeartbeat"));
					break;
				}

				case MessageTypes.OrderRegister:
				{
					ProcessOrderRegisterMessage((OrderRegisterMessage)message);
					break;
				}

				case MessageTypes.OrderCancel:
				{
					ProcessCancelMessage((OrderCancelMessage)message);
					break;
				}

				case MessageTypes.OrderReplace:
				{
					ProcessOrderReplaceMessage((OrderReplaceMessage)message);
					break;
				}

				case MessageTypes.SecurityLookup:
				{
					ProcessSecurityLookupMessage((SecurityLookupMessage)message);
					break;
				}

				case MessageTypes.MarketData:
				{
					ProcessMarketDataMessage((MarketDataMessage)message);
					break;
				}
			}
		}
	}
}