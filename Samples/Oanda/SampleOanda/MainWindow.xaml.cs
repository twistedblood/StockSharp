namespace SampleOanda
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Security;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.BusinessEntities;
	using StockSharp.Oanda;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		private bool _isConnected;

		public OandaTrader Trader;

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly TradesWindow _tradesWindow = new TradesWindow();
		private readonly MyTradesWindow _myTradesWindow = new MyTradesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();
		private readonly PortfoliosWindow _portfoliosWindow = new PortfoliosWindow();
		private readonly StopOrdersWindow _stopOrdersWindow = new StopOrdersWindow();

		public MainWindow()
		{
			InitializeComponent();

			Title = Title.Put("OANDA");

			_ordersWindow.MakeHideable();
			_myTradesWindow.MakeHideable();
			_tradesWindow.MakeHideable();
			_securitiesWindow.MakeHideable();
			_stopOrdersWindow.MakeHideable();
			_portfoliosWindow.MakeHideable();

			Instance = this;
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_ordersWindow.DeleteHideable();
			_myTradesWindow.DeleteHideable();
			_tradesWindow.DeleteHideable();
			_securitiesWindow.DeleteHideable();
			_stopOrdersWindow.DeleteHideable();
			_portfoliosWindow.DeleteHideable();
			
			_securitiesWindow.Close();
			_tradesWindow.Close();
			_myTradesWindow.Close();
			_stopOrdersWindow.Close();
			_ordersWindow.Close();
			_portfoliosWindow.Close();

			if (Trader != null)
				Trader.Dispose();

			base.OnClosing(e);
		}

		public static MainWindow Instance { get; private set; }

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (!_isConnected)
			{
				if (Token.Password.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str3836);
					return;
				}

				if (Trader == null)
				{
					// create connector
					Trader = new OandaTrader();

					Trader.Restored += () => this.GuiAsync(() =>
					{
						// update gui labes
						ChangeConnectStatus(true);
						MessageBox.Show(this, LocalizedStrings.Str2958);
					});

					// subscribe on connection successfully event
					Trader.Connected += () =>
					{
						// set flag (connection is established)
						_isConnected = true;

						// update gui labes
						this.GuiAsync(() => ChangeConnectStatus(true));
					};
					Trader.Disconnected += () => this.GuiAsync(() => ChangeConnectStatus(false));

					// subscribe on connection error event
					Trader.ConnectionError += error => this.GuiAsync(() =>
					{
						// update gui labes
						ChangeConnectStatus(false);

						MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
					});

					// subscribe on error event
					Trader.Error += error =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

					// subscribe on error of market data subscription event
					Trader.MarketDataSubscriptionFailed += (security, type, error) =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(type, security)));

					Trader.NewSecurities += securities => _securitiesWindow.SecurityPicker.Securities.AddRange(securities);
					Trader.NewMyTrades += trades => _myTradesWindow.TradeGrid.Trades.AddRange(trades);
					Trader.NewTrades += trades => _tradesWindow.TradeGrid.Trades.AddRange(trades);
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

					ShowSecurities.IsEnabled = ShowTrades.IsEnabled =
					ShowMyTrades.IsEnabled = ShowOrders.IsEnabled = 
					ShowPortfolios.IsEnabled = ShowStopOrders.IsEnabled = true;
				}

				Trader.Token = Token.Password.To<SecureString>();
				Trader.Server = IsDemo.IsChecked == true ? OandaServers.Practice : OandaServers.Real;

				// clear password box for security reason
				//Token.Clear();

				Trader.Connect();
			}
			else
			{
				Trader.Disconnect();
			}
		}

		private void OrdersFailed(IEnumerable<OrderFail> fails)
		{
			this.GuiAsync(() =>
			{
				foreach (var fail in fails)
					MessageBox.Show(this, fail.Error.ToString(), LocalizedStrings.Str2960);
			});
		}

		private void ChangeConnectStatus(bool isConnected)
		{
			_isConnected = isConnected;
			ConnectBtn.Content = isConnected ? LocalizedStrings.Disconnect : LocalizedStrings.Connect;
		}

		private void ShowSecuritiesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_securitiesWindow);
		}

		private void ShowTradesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_tradesWindow);
		}

		private void ShowMyTradesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_myTradesWindow);
		}

		private void ShowOrdersClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_ordersWindow);
		}

		private void ShowPortfoliosClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_portfoliosWindow);
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
	}
}
