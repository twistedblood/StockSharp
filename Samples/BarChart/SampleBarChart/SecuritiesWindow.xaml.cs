namespace SampleBarChart
{
	using System;
	using System.Collections.Generic;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	public partial class SecuritiesWindow
	{
		private readonly SynchronizedDictionary<Security, QuotesWindow> _quotesWindows = new SynchronizedDictionary<Security, QuotesWindow>();
		private readonly SynchronizedDictionary<Security, HistoryTicksWindow> _historyTicksWindows = new SynchronizedDictionary<Security, HistoryTicksWindow>();
		private readonly SynchronizedDictionary<Security, HistoryCandlesWindow> _historyCandlesWindows = new SynchronizedDictionary<Security, HistoryCandlesWindow>();
		private bool _initialized;

		public SecuritiesWindow()
		{
			InitializeComponent();
		}

		protected override void OnClosed(EventArgs e)
		{
			var trader = MainWindow.Instance.Trader;
			if (trader != null)
			{
				if (_initialized)
				{
					trader.MarketDepthsChanged -= TraderOnMarketDepthsChanged;
				}

				_quotesWindows.ForEach(pair =>
				{
					trader.UnRegisterMarketDepth(pair.Key);
					DeleteHideableAndClose(pair.Value);
				});

				_historyTicksWindows.ForEach(pair => DeleteHideableAndClose(pair.Value));
				_historyCandlesWindows.ForEach(pair => DeleteHideableAndClose(pair.Value));
			}

			base.OnClosed(e);
		}

		private static void DeleteHideableAndClose(Window window)
		{
			window.DeleteHideable();
			window.Close();
		}

		public Security SelectedSecurity
		{
			get { return SecurityPicker.SelectedSecurity; }
		}

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			HistoryTicks.IsEnabled = HistoryCandles.IsEnabled = Depth.IsEnabled = security != null;
		}

		private void DepthClick(object sender, RoutedEventArgs e)
		{
			var trader = MainWindow.Instance.Trader;

			var window = _quotesWindows.SafeAdd(SelectedSecurity, security =>
			{
				// subscribe on order book flow
				trader.RegisterMarketDepth(security);

				// create order book window
				var wnd = new QuotesWindow { Title = security.Id + " " + LocalizedStrings.MarketDepth };
				wnd.MakeHideable();
				return wnd;
			});

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();

			TryInitialize();
		}

		private void TryInitialize()
		{
			if (!_initialized)
			{
				_initialized = true;

				var trader = MainWindow.Instance.Trader;

				trader.MarketDepthsChanged += TraderOnMarketDepthsChanged;

				TraderOnMarketDepthsChanged(new[] { trader.GetMarketDepth(SecurityPicker.SelectedSecurity) });
			}
		}

		private void TraderOnMarketDepthsChanged(IEnumerable<MarketDepth> depths)
		{
			foreach (var depth in depths)
			{
				var wnd = _quotesWindows.TryGetValue(depth.Security);

				if (wnd != null)
					wnd.DepthCtrl.UpdateDepth(depth);
			}
		}

		private void HistoryTicksClick(object sender, RoutedEventArgs e)
		{
			var window = _historyTicksWindows.SafeAdd(SelectedSecurity, security =>
			{
				// create historical ticks window
				var wnd = new HistoryTicksWindow(SelectedSecurity);
				wnd.MakeHideable();
				return wnd;
			});

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();
		}

		private void HistoryCandlesClick(object sender, RoutedEventArgs e)
		{
			var window = _historyCandlesWindows.SafeAdd(SelectedSecurity, security =>
			{
				// create historical candles window
				var wnd = new HistoryCandlesWindow(SelectedSecurity);
				wnd.MakeHideable();
				return wnd;
			});

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();
		}

		private void FindClick(object sender, RoutedEventArgs e)
		{
			new FindSecurityWindow().ShowModal(this);
		}
	}
}