namespace SampleSmartSMA
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Media;

	using MoreLinq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Strategies.Reporting;
	using StockSharp.Algo.Indicators;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.SmartCom;
	using StockSharp.Xaml;
	using StockSharp.Xaml.Charting;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		private bool _isConnected;
		private readonly TimeSpan _timeFrame = SmartComTimeFrames.Minute5;
		private readonly SynchronizedList<TimeFrameCandle> _historyCandles = new SynchronizedList<TimeFrameCandle>();
		private readonly LogManager _logManager = new LogManager();
		private CandleManager _candleManager;
		private SmartTrader _trader;
		private SmaStrategy _strategy;
		private DateTimeOffset _lastHistoryCandle;
		private Security _lkoh;
		private readonly ChartArea _area;
		private ChartCandleElement _candlesElem;
		private ChartIndicatorElement _longMaElem;
		private ChartIndicatorElement _shortMaElem;
        
		public MainWindow()
		{
			InitializeComponent();

			_logManager.Listeners.Add(new GuiLogListener(LogControl));

			_area = new ChartArea();
			_chart.Areas.Add(_area);
		}

		private void OrdersOrderSelected(object sender, SelectionChangedEventArgs e)
		{
			CancelOrders.IsEnabled = !_orders.SelectedOrders.IsEmpty();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (_trader != null)
				_trader.Dispose();

			base.OnClosing(e);
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (!_isConnected)
			{
				if (Login.Text.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str2974);
					return;
				}
				else if (Password.Password.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str2975);
					return;
				}

				if (_trader == null)
				{
					// создаем подключение
					_trader = new SmartTrader();

					_logManager.Sources.Add(_trader);

					Portfolios.Connector = _trader;

					// подписываемся на событие успешного соединения
					_trader.Connected += () =>
					{
						// возводим флаг, что соединение установлено
						_isConnected = true;

						// разблокируем кнопку Экспорт
						this.GuiAsync(() => ChangeConnectStatus(true));

						_candleManager = new CandleManager(_trader);

						_trader.NewCandles += (series, candles) => _historyCandles.SyncDo(col =>
						{
							_historyCandles.AddRange(candles.Cast<TimeFrameCandle>());

							foreach (var candle in candles)
								ProcessCandle(candle);
						});

						_trader.NewSecurities += securities =>
						{
							// находим нужную бумагу
							var lkoh = securities.FirstOrDefault(s => s.Code == "LKOH");

							if (lkoh != null)
							{
								_lkoh = lkoh;

								this.GuiAsync(() =>
								{
									Start.IsEnabled = true;
								});
							}
						};

						_trader.NewMyTrades += trades =>
						{
							if (_strategy != null)
							{
								// найти те сделки, которые совершила стратегия скользящей средней
								trades = trades.Where(t => _strategy.Orders.Any(o => o == t.Order));

								_trades.Trades.AddRange(trades);
							}
						};

						// подписываемся на событие о неудачной регистрации заявок
						//_trader.OrdersRegisterFailed += OrdersFailed;

						_candleManager.Processing += (s, candle) =>
						{
							// выводим только те свечи, которые не были отрисованы как исторические
							if (candle.OpenTime > _lastHistoryCandle)
								ProcessCandle(candle);
						};

						this.GuiAsync(() =>
						{
							ConnectBtn.IsEnabled = false;
						});
					};

					// подписываемся на событие разрыва соединения
					_trader.ConnectionError += error => this.GuiAsync(() =>
					{
						// заблокируем кнопку Экспорт (так как соединение было потеряно)
						ChangeConnectStatus(false);

						MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
					});

					_trader.Disconnected += () => this.GuiAsync(() => ChangeConnectStatus(false));

					// подписываемся на ошибку обработки данных (транзакций и маркет)
					//_trader.Error += error => this.GuiAsync(() => MessageBox.Show(this, error.ToString(), "Ошибка обработки данных"));

					// подписываемся на ошибку подписки маркет-данных
					_trader.MarketDataSubscriptionFailed += (security, type, error) =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(type, security)));
				}

				_trader.Login = Login.Text;
				_trader.Password = Password.Password;
				_trader.Address = Address.SelectedAddress;

				// очищаем из текстового поля в целях безопасности
				//Password.Clear();

				_trader.Connect();
			}
			else
			{
				_trader.Disconnect();
			}
		}

		//private void OrdersFailed(IEnumerable<OrderFail> fails)
		//{
		//	this.GuiAsync(() =>
		//	{
		//		foreach (var fail in fails)
		//			MessageBox.Show(this, fail.Error.ToString(), "Ошибка регистрации заявки");
		//	});
		//}

		private void ChangeConnectStatus(bool isConnected)
		{
			_isConnected = isConnected;
			ConnectBtn.Content = isConnected ? LocalizedStrings.Disconnect : LocalizedStrings.Connect;
		}

		//private void OnLog(LogMessage message)
		//{
		//	// если стратегия вывела не просто сообщение, то вывести на экран.
		//	if (message.Level != LogLevels.Info && message.Level != LogLevels.Debug)
		//		this.GuiAsync(() => MessageBox.Show(this, message.Message));
		//}

		private void OnStrategyPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			this.GuiAsync(() =>
			{
				Status.Content = _strategy.ProcessState;
				PnL.Content = _strategy.PnL;
				Slippage.Content = _strategy.Slippage;
				Position.Content = _strategy.Position;
				Latency.Content = _strategy.Latency;
			});
		}

		private void CancelOrdersClick(object sender, RoutedEventArgs e)
		{
			_orders.SelectedOrders.ForEach(_trader.CancelOrder);
		}

		private void StartClick(object sender, RoutedEventArgs e)
		{
			// если были получены и инструмент, и портфель
			if (_strategy == null)
			{
				if (Portfolios.SelectedPortfolio == null)
				{
					MessageBox.Show(this, LocalizedStrings.Str3009);
					return;
				}

				// создаем скользящие средние, на 80 5-минуток и 10 5-минуток
				var longSma = new SimpleMovingAverage { Length = 80 };
				var shortSma = new SimpleMovingAverage { Length = 10 };

				// регистрируем наш тайм-фрейм
				var series = new CandleSeries(typeof(TimeFrameCandle), _lkoh, _timeFrame);

				// создаем торговую стратегию
				_strategy = new SmaStrategy(series, longSma, shortSma)
				{
					Volume = 1,
					Security = _lkoh,
					Portfolio = Portfolios.SelectedPortfolio,
					Connector = _trader,
				};
				_logManager.Sources.Add(_strategy);
				//_strategy.Log += OnLog;
				_strategy.PropertyChanged += OnStrategyPropertyChanged;

				_candlesElem = new ChartCandleElement();
				_area.Elements.Add(_candlesElem);

				_longMaElem = new ChartIndicatorElement
				{
					Title = LocalizedStrings.Long,
					Color = Colors.OrangeRed
				};
				_area.Elements.Add(_longMaElem);

				_shortMaElem = new ChartIndicatorElement
				{
					Title = LocalizedStrings.Short,
					Color = Colors.RoyalBlue
				};
				_area.Elements.Add(_shortMaElem);

				var marketTime = _trader.CurrentTime;

				// начинаем получать свечи за период в 5 дней
				_candleManager.Start(series, DateTime.Today - TimeSpan.FromDays(5), marketTime);

				_lastHistoryCandle = _timeFrame.GetCandleBounds(marketTime).Min;

				Report.IsEnabled = true;
			}

			if (_strategy.ProcessState == ProcessStates.Stopped)
			{
				// запускаем процесс получения стакана, необходимый для работы алгоритма котирования
				_trader.RegisterMarketDepth(_strategy.Security);
				_strategy.Start();
				Start.Content = LocalizedStrings.Str242;
			}
			else
			{
				_trader.UnRegisterMarketDepth(_strategy.Security);
				_strategy.Stop();
				Start.Content = LocalizedStrings.Str2421;
			}
		}

		private void ProcessCandle(Candle candle)
		{
			var longValue = candle.State == CandleStates.Finished ? _strategy.LongSma.Process(candle) : null;
			var shortValue = candle.State == CandleStates.Finished ? _strategy.ShortSma.Process(candle) : null;

			_chart.Draw(candle.OpenTime, new Dictionary<IChartElement, object>
			{
				{ _candlesElem, candle },
				{ _longMaElem, longValue },
				{ _shortMaElem, shortValue },
			});
		}

		private void ReportClick(object sender, RoutedEventArgs e)
		{
			// сгерерировать отчет по прошедшему тестированию
			new ExcelStrategyReport(_strategy, "sma.xlsx").Generate();

			// открыть отчет
			Process.Start("sma.xlsx");
		}
	}
}