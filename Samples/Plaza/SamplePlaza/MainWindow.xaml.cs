namespace SamplePlaza
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Plaza;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		private bool _isConnected;

		public readonly PlazaTrader Trader = new PlazaTrader();
		private bool _initialized;

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly TradesWindow _tradesWindow = new TradesWindow();
		private readonly MyTradesWindow _myTradesWindow = new MyTradesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();
		private readonly OrdersLogWindow _ordersLogWindow = new OrdersLogWindow();
		private readonly PortfoliosWindow _portfoliosWindow = new PortfoliosWindow();

		private readonly LogManager _logManager = new LogManager();

		public MainWindow()
		{
			InitializeComponent();
			Instance = this;

			Title = Title.Put("Plaza II");

			_ordersWindow.MakeHideable();
			_ordersLogWindow.MakeHideable();
			_myTradesWindow.MakeHideable();
			_tradesWindow.MakeHideable();
			_securitiesWindow.MakeHideable();
			_portfoliosWindow.MakeHideable();

			AppName.Text = Trader.AppName;
			CGateKey.Text = Trader.CGateKey;

			Tables.SelectedTables = Trader.Tables.Select(t => t.Id);

			_logManager.Sources.Add(Trader);
			_logManager.Listeners.Add(new FileLogListener { LogDirectory = "StockSharp_Plaza" });
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_ordersWindow.DeleteHideable();
			_ordersLogWindow.DeleteHideable();
			_myTradesWindow.DeleteHideable();
			_tradesWindow.DeleteHideable();
			_securitiesWindow.DeleteHideable();
			_portfoliosWindow.DeleteHideable();
			
			_securitiesWindow.Close();
			_tradesWindow.Close();
			_myTradesWindow.Close();
			_ordersWindow.Close();
			_ordersLogWindow.Close();
			_portfoliosWindow.Close();

			Trader.Dispose();

			base.OnClosing(e);
		}

		public static MainWindow Instance { get; private set; }

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!_isConnected)
				{
					Trader.Address = Address.Text.To<EndPoint>();
					Trader.IsCGate = IsCGate.IsChecked == true;
					Trader.CGateKey = CGateKey.Text;
					Trader.AppName = AppName.Text;
					Trader.TableRegistry.StreamRegistry.IsFastRepl = IsFastRepl.IsChecked == true;

					if (IsAutorization.IsChecked == true)
					{
						Trader.Login = Login.Text;
						Trader.Password = Password.Password;
					}
					else
					{
						Trader.Login = string.Empty;
						Trader.Password = string.Empty;
					}

					if (!_initialized)
					{
						_initialized = true;

						var revisionManager = Trader.StreamManager.RevisionManager;

						revisionManager.Tables.Add(Trader.TableRegistry.IndexLog);
						revisionManager.Tables.Add(Trader.TableRegistry.TradeFuture);
						revisionManager.Tables.Add(Trader.TableRegistry.TradeOption);

						Trader.Tables.Clear();
						Trader.TableRegistry.SyncTables(Tables.SelectedTables);

						if (Trader.Tables.Contains(Trader.TableRegistry.AnonymousOrdersLog))
						{
							Trader.CreateDepthFromOrdersLog = true;
						}

						Trader.ReConnectionSettings.AttemptCount = -1;
						Trader.Restored += () => this.GuiAsync(() => MessageBox.Show(this, LocalizedStrings.Str2958));

						// подписываемся на событие успешного соединения
						Trader.Connected += () =>
						{
							this.GuiAsync(() => ChangeConnectStatus(true));
						};

						// подписываемся на событие разрыва соединения
						Trader.ConnectionError += error => this.GuiAsync(() =>
						{
							ChangeConnectStatus(false);
							MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
						});

						// подписываемся на событие успешного отключения
						Trader.Disconnected += () => this.GuiAsync(() => ChangeConnectStatus(false));

						// подписываемся на ошибку обработки данных (транзакций и маркет)
						//Trader.Error += error =>
						//	this.GuiAsync(() => MessageBox.Show(this, error.ToString(), "Ошибка обработки данных"));

						// подписываемся на ошибку подписки маркет-данных
						Trader.MarketDataSubscriptionFailed += (security, type, error) =>
							this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(type, security)));

						Trader.NewSecurities += securities => _securitiesWindow.SecurityPicker.Securities.AddRange(securities);
						Trader.NewTrades += trades => _tradesWindow.TradeGrid.Trades.AddRange(trades);
						Trader.NewOrders +=orders => _ordersWindow.OrderGrid.Orders.AddRange(orders);
						Trader.NewMyTrades += trades => _myTradesWindow.TradeGrid.Trades.AddRange(trades);
						Trader.NewOrderLogItems += items => items.ForEach(_ordersLogWindow.AddOperation);

						Trader.NewPortfolios += portfolios => _portfoliosWindow.PortfolioGrid.Portfolios.AddRange(portfolios);
						Trader.NewPositions += positions => _portfoliosWindow.PortfolioGrid.Positions.AddRange(positions);

						// подписываемся на событие о неудачной регистрации заявок
						Trader.OrdersRegisterFailed += OrdersFailed;

						// подписываемся на событие о неудачном снятии заявок
						Trader.OrdersCancelFailed += OrdersFailed;

						// устанавливаем поставщик маркет-данных
						_securitiesWindow.SecurityPicker.MarketDataProvider = Trader;
					}

					Trader.Connect();
				}
				else
				{
					Trader.Disconnect();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message, LocalizedStrings.Str152);
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
			connectionStatus.Content = isConnected ? LocalizedStrings.Connected : LocalizedStrings.Disconnected;
            
            ShowSecurities.IsEnabled = ShowTrades.IsEnabled =
            ShowMyTrades.IsEnabled = ShowOrders.IsEnabled =
            ShowPortfolios.IsEnabled = isConnected;

			ShowOrdersLog.IsEnabled = isConnected;

			IsCGate.IsEnabled = IsFastRepl.IsEnabled = IsAutorization.IsEnabled = Tables.IsEnabled = !isConnected;
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

		private void ShowOrdersLogClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_ordersLogWindow);
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
