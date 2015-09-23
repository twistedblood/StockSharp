namespace SampleETrade
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Reflection;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Messages;
	using StockSharp.BusinessEntities;
	using StockSharp.ETrade;
	using StockSharp.ETrade.Native;
	using StockSharp.Logging;
	using StockSharp.Xaml;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		public static MainWindow Instance { get; private set; }

		public static readonly DependencyProperty IsConnectedProperty = DependencyProperty.Register("IsConnected", typeof(bool), typeof(MainWindow), new PropertyMetadata(default(bool)));

		public bool IsConnected
		{
			get { return (bool)GetValue(IsConnectedProperty); }
			set { SetValue(IsConnectedProperty, value); }
		}

		public ETradeTrader Trader { get; private set; }

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();
		private readonly PortfoliosWindow _portfoliosWindow = new PortfoliosWindow();
		private readonly StopOrdersWindow _stopOrdersWindow = new StopOrdersWindow();
		private readonly MyTradesWindow _myTradesWindow = new MyTradesWindow();

		//private Security[] _securities;

		private static string ConsumerKey
		{
			get { return Properties.Settings.Default.ConsumerKey; }
		}

		private static bool IsSandbox
		{
			get { return Properties.Settings.Default.Sandbox; }
		}

		private readonly LogManager _logManager = new LogManager();

		public MainWindow()
		{
			Instance = this;
			InitializeComponent();

			Title = Title.Put("E*TRADE");

			Closing += OnClosing;

			_ordersWindow.MakeHideable();
			_securitiesWindow.MakeHideable();
			_stopOrdersWindow.MakeHideable();
			_portfoliosWindow.MakeHideable();
			_myTradesWindow.MakeHideable();

			var guilistener = new GuiLogListener(_logControl);
			guilistener.Filters.Add(msg => msg.Level > LogLevels.Debug);
			_logManager.Listeners.Add(guilistener);

			var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var path = Path.Combine(location ?? "", "ETrade", "restdump_{0:yyyyMMdd-HHmmss}.log".Put(DateTime.Now));

			_logManager.Listeners.Add(new FileLogListener(path));

			Application.Current.MainWindow = this;
		}

		private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
		{
			Properties.Settings.Default.Save();
			_ordersWindow.DeleteHideable();
			_securitiesWindow.DeleteHideable();
			_stopOrdersWindow.DeleteHideable();
			_portfoliosWindow.DeleteHideable();
			_myTradesWindow.DeleteHideable();

			_securitiesWindow.Close();
			_stopOrdersWindow.Close();
			_ordersWindow.Close();
			_portfoliosWindow.Close();
			_myTradesWindow.Close();

			if (Trader != null)
				Trader.Dispose();
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			var secret = PwdBox.Password;

			if (!IsConnected)
			{
				if (ConsumerKey.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str3689);
					return;
				}
				if (secret.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str3690);
					return;
				}

				if (Trader == null)
				{
					// create connector
					Trader = new ETradeTrader();

					try
					{
						Trader.AccessToken = null;

						var token = OAuthToken.Deserialize(Properties.Settings.Default.AccessToken);
						if (token != null && token.ConsumerKey.ToLowerInvariant() == ConsumerKey.ToLowerInvariant())
							Trader.AccessToken = token;
					}
					catch (Exception ex)
					{
						MessageBox.Show(this, LocalizedStrings.Str3691Params.Put(ex));
					}

					Trader.LogLevel = LogLevels.Debug;

					_logManager.Sources.Add(Trader);

					// subscribe on connection successfully event
					Trader.Connected += () => this.GuiAsync(() =>
					{
						Properties.Settings.Default.AccessToken = Trader.AccessToken.Serialize();
						OnConnectionChanged(true);
					});

					// subscribe on connection error event
					Trader.ConnectionError += error => this.GuiAsync(() =>
					{
						OnConnectionChanged(Trader.ConnectionState == ConnectionStates.Connected);
						MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
					});

					Trader.Disconnected += () => this.GuiAsync(() => OnConnectionChanged(false));

					// subscribe on error event
					Trader.Error += error =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

					// subscribe on error of market data subscription event
					Trader.MarketDataSubscriptionFailed += (security, type, error) =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(type, security)));

					Trader.NewSecurities += securities => _securitiesWindow.SecurityPicker.Securities.AddRange(securities);
					Trader.NewMyTrades += trades => _myTradesWindow.TradeGrid.Trades.AddRange(trades);
					Trader.NewOrders += orders => _ordersWindow.OrderGrid.Orders.AddRange(orders);
					Trader.NewStopOrders += orders => _stopOrdersWindow.OrderGrid.Orders.AddRange(orders);
					Trader.NewPortfolios += portfolios =>
					{
						// subscribe on portfolio updates
						portfolios.ForEach(Trader.RegisterPortfolio);

						_portfoliosWindow.PortfolioGrid.Portfolios.AddRange(portfolios);
					};
					Trader.NewPositions += positions => _portfoliosWindow.PortfolioGrid.Positions.AddRange(positions);

					// subscribe on error of order registration event
					Trader.OrdersRegisterFailed += OrdersFailed;
					// subscribe on error of order cancelling event
					Trader.OrdersCancelFailed += OrdersFailed;

					// subscribe on error of stop-order registration event
					Trader.StopOrdersRegisterFailed += OrdersFailed;
					// subscribe on error of stop-order cancelling event
					Trader.StopOrdersCancelFailed += OrdersFailed;

					// set market data provider
					_securitiesWindow.SecurityPicker.MarketDataProvider = Trader;
				}

				Trader.Sandbox = IsSandbox;
				//Trader.SandboxSecurities = IsSandbox ? GetSandboxSecurities() : null;
				Trader.ConsumerKey = ConsumerKey;
				Trader.ConsumerSecret = secret;

				Trader.Connect();
			}
			else
			{
				Trader.Disconnect();
			}
		}

		private void OnConnectionChanged(bool isConnected)
		{
			IsConnected = isConnected;
			ConnectBtn.Content = isConnected ? LocalizedStrings.Disconnect : LocalizedStrings.Connect;
		}

		private void OrdersFailed(IEnumerable<OrderFail> fails)
		{
			this.GuiAsync(() =>
			{
				foreach(var fail in fails)
				{
					var msg = fail.Error.ToString();
					MessageBox.Show(this, msg, LocalizedStrings.Str2960);
				}
			});
		}

		private void ShowMyTradesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_myTradesWindow);
		}

		private void ShowSecuritiesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_securitiesWindow);
		}

		private void ShowPortfoliosClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_portfoliosWindow);
		}

		private void ShowOrdersClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_ordersWindow);
		}

		private void ShowStopOrdersClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_stopOrdersWindow);
		}

		private static void ShowOrHide(Window window)
		{
			if (window == null)
				throw new ArgumentNullException("window");

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();
		}

		//private Security[] GetSandboxSecurities()
		//{
		//	return _securities ?? (_securities = new[]
		//	{
		//		new Security
		//		{
		//			Id = "CSCO@EQ", Code = "CSCO", Name = "CISCO SYS INC", ExchangeBoard = ExchangeBoard.Test,
		//			Decimals = 2, VolumeStep = 1, StepPrice = 0.01m, PriceStep = 0.01m
		//		},
		//		new Security
		//		{
		//			Id = "IBM@EQ", Code = "IBM", Name = "INTERNATIONAL BUSINESS MACHS COM", ExchangeBoard = ExchangeBoard.Test,
		//			Decimals = 2, VolumeStep = 1, StepPrice = 0.01m, PriceStep = 0.01m
		//		},
		//		new Security
		//		{
		//			Id = "MSFT@EQ", Code = "MSFT", Name = "MICROSOFT CORP COM", ExchangeBoard = ExchangeBoard.Test,
		//			Decimals = 2, VolumeStep = 1, StepPrice = 0.01m, PriceStep = 0.01m
		//		}
		//	});
		//}
	}
}
