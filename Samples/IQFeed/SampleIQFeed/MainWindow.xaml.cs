namespace SampleIQFeed
{
	using System;
	using System.ComponentModel;
	using System.Net;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Xaml;

	using StockSharp.Messages;
	using StockSharp.BusinessEntities;
	using StockSharp.IQFeed;
	using StockSharp.Logging;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		private bool _isInitialized;

		public readonly IQFeedTrader Trader;

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly NewsWindow _newsWindow = new NewsWindow();

		private readonly LogManager _logManager = new LogManager();

		public MainWindow()
		{
			InitializeComponent();

			Title = Title.Put("IQFeed");

			Trader = new IQFeedTrader();
			//{
			//	LogLevel = LogLevels.Debug,
			//	MarketDataAdapter = { LogLevel = LogLevels.Debug }
			//};

			ConfigManager.RegisterService<IConnector>(Trader);

			Level1AddressCtrl.Text = Trader.Level1Address.To<string>();
			Level2AddressCtrl.Text = Trader.Level2Address.To<string>();
			LookupAddressCtrl.Text = Trader.LookupAddress.To<string>();
			AdminAddressCtrl.Text = Trader.AdminAddress.To<string>();

			DownloadSecurityFromSiteCtrl.IsChecked = Trader.IsDownloadSecurityFromSite;

			_securitiesWindow.MakeHideable();
			_newsWindow.MakeHideable();

			Instance = this;

			_logManager.Listeners.Add(new FileLogListener("log.txt"));
			_logManager.Sources.Add(Trader);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_securitiesWindow.DeleteHideable();
			_newsWindow.DeleteHideable();

			_securitiesWindow.Close();
			_newsWindow.Close();

			Trader.Dispose();

			base.OnClosing(e);
		}

		public static MainWindow Instance { get; private set; }

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (!_isInitialized)
			{
				_isInitialized = true;

				// subscribe on connection successfully event
				Trader.Connected += () =>
				{
					Trader.RegisterNews();

					// update gui labes
					this.GuiAsync(() => ChangeConnectStatus(true));
				};
				Trader.Disconnected += () => this.GuiAsync(() => ChangeConnectStatus(false));

				// subscribe on connection error event
				Trader.ConnectionError += error => this.GuiAsync(() =>
				{
					// update gui labes
					this.GuiAsync(() => ChangeConnectStatus(false));

					MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
				});

				// subscribe on error event
				Trader.Error += error =>
					this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

				// subscribe on error of market data subscription event
				Trader.MarketDataSubscriptionFailed += (security, type, error) =>
					this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(type, security)));

				Trader.NewSecurities += securities => _securitiesWindow.SecurityPicker.Securities.AddRange(securities);
				Trader.NewNews += news => _newsWindow.NewsGrid.News.Add(news);

				// set market data provider
				_securitiesWindow.SecurityPicker.MarketDataProvider = Trader;

				ShowNews.IsEnabled = ShowSecurities.IsEnabled = true;
			}

			if (Trader.ConnectionState == ConnectionStates.Disconnected || Trader.ConnectionState == ConnectionStates.Failed)
			{
				// set connection settings
				Trader.Level1Address = Level1AddressCtrl.Text.To<EndPoint>();
				Trader.Level2Address = Level2AddressCtrl.Text.To<EndPoint>();
				Trader.LookupAddress = LookupAddressCtrl.Text.To<EndPoint>();
				Trader.AdminAddress = AdminAddressCtrl.Text.To<EndPoint>();

				Trader.IsDownloadSecurityFromSite = DownloadSecurityFromSiteCtrl.IsChecked == true;

				Trader.Connect();	
			}
			else
				Trader.Disconnect();
		}

		private void ChangeConnectStatus(bool isConnected)
		{
			ConnectBtn.Content = isConnected ? LocalizedStrings.Disconnect : LocalizedStrings.Connect;
		}

		private void ShowSecuritiesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_securitiesWindow);
		}

		private void ShowNewsClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_newsWindow);
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