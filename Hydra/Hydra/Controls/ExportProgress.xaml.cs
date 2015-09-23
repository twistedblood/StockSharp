namespace StockSharp.Hydra.Controls
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Xaml;
	using Ecng.Xaml.Database;

	using StockSharp.Algo;
	using StockSharp.Algo.Export;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Hydra.Windows;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	public partial class ExportProgress
	{
		private BackgroundWorker _worker;
		private ExportButton _button;
		private Grid _mainGrid;
		private string _fileName;

		public ExportProgress()
		{
			InitializeComponent();
		}

		public bool IsStarted
		{
			get { return _worker != null; }
		}

		public void Init(ExportButton button, Grid mainGrid)
		{
			if (button == null)
				throw new ArgumentNullException("button");

			if (mainGrid == null)
				throw new ArgumentNullException("mainGrid");

			_button = button;
			_mainGrid = mainGrid;
		}

		public void Start(Security security, Type dataType, object arg, IEnumerableEx values, object path)
		{
			if (dataType == null)
				throw new ArgumentNullException("dataType");

			if (security == null && dataType != typeof(NewsMessage) && dataType != typeof(SecurityMessage))
				throw new ArgumentNullException("security");

			if (values == null)
				throw new ArgumentNullException("values");

			var currProgress = 5;

			var valuesPerPercent = (values.Count / (100 - currProgress)).Max(1);
			var valuesPerCount = (valuesPerPercent / 10).Max(1);
			var currCount = 0;
			var valuesCount = 0;

			Func<int, bool> isCancelled = count =>
			{
				var isCancelling = _worker.CancellationPending;

				if (!isCancelling)
				{
					valuesCount += count;

					if (valuesCount / valuesPerPercent > currProgress)
					{
						currProgress = valuesCount / valuesPerPercent;
						_worker.ReportProgress(currProgress);
					}

					if (valuesCount > currCount)
					{
						currCount = valuesCount + valuesPerCount;
						this.GuiAsync(() => UpdateCount(valuesCount));
					}
				}

				return isCancelling;
			};

			string fileName;
			BaseExporter exporter;

			switch (_button.ExportType)
			{
				case ExportTypes.Excel:
					fileName = (string)path;
					exporter = new ExcelExporter(security, arg, isCancelled, fileName, () => GuiDispatcher.GlobalDispatcher.AddSyncAction(TooManyValues));
					break;
				case ExportTypes.Xml:
					fileName = (string)path;
					exporter = new XmlExporter(security, arg, isCancelled, fileName);
					break;
				case ExportTypes.Txt:
					var wnd = new ExportTxtPreviewWindow
					{
						DataType = dataType,
						Arg = arg
					};

					var registry = ((HydraEntityRegistry)ConfigManager.GetService<IEntityRegistry>()).Settings.TemplateTxtRegistry;

					if (dataType == typeof(SecurityMessage))
						wnd.TxtTemplate = registry.TemplateTxtSecurity;
					else if (dataType == typeof(NewsMessage))
						wnd.TxtTemplate = registry.TemplateTxtNews;
					else if (dataType.IsSubclassOf(typeof(CandleMessage)))
						wnd.TxtTemplate = registry.TemplateTxtCandle;
					else if (dataType == typeof(Level1ChangeMessage))
						wnd.TxtTemplate = registry.TemplateTxtLevel1;
					else if (dataType == typeof(QuoteChangeMessage))
						wnd.TxtTemplate = registry.TemplateTxtDepth;
					else if (dataType == typeof(ExecutionMessage))
					{
						if (arg == null)
							throw new ArgumentNullException("arg");

						switch ((ExecutionTypes)arg)
						{
							case ExecutionTypes.Tick:
								wnd.TxtTemplate = registry.TemplateTxtTick;
								break;
							case ExecutionTypes.Order:
							case ExecutionTypes.Trade:
								wnd.TxtTemplate = registry.TemplateTxtTransaction;
								break;
							case ExecutionTypes.OrderLog:
								wnd.TxtTemplate = registry.TemplateTxtOrderLog;
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}
					}
					else
						throw new ArgumentOutOfRangeException("dataType", dataType, LocalizedStrings.Str721);


					if (!wnd.ShowModal(this))
						return;

					if (dataType == typeof(SecurityMessage))
						registry.TemplateTxtSecurity = wnd.TxtTemplate;
					else if (dataType == typeof(NewsMessage))
						registry.TemplateTxtNews = wnd.TxtTemplate;
					else if (dataType.IsSubclassOf(typeof(CandleMessage)))
						registry.TemplateTxtCandle = wnd.TxtTemplate;
					else if (dataType == typeof(Level1ChangeMessage))
						registry.TemplateTxtLevel1 = wnd.TxtTemplate;
					else if (dataType == typeof(QuoteChangeMessage))
						registry.TemplateTxtDepth = wnd.TxtTemplate;
					else if (dataType == typeof(ExecutionMessage))
					{
						if (arg == null)
							throw new ArgumentNullException("arg");

						switch ((ExecutionTypes)arg)
						{
							case ExecutionTypes.Tick:
								registry.TemplateTxtTick = wnd.TxtTemplate;
								break;
							case ExecutionTypes.Order:
							case ExecutionTypes.Trade:
								registry.TemplateTxtTransaction = wnd.TxtTemplate;
								break;
							case ExecutionTypes.OrderLog:
								registry.TemplateTxtOrderLog = wnd.TxtTemplate;
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}
					}
					else
						throw new ArgumentOutOfRangeException("dataType", dataType, LocalizedStrings.Str721);

					fileName = (string)path;
					exporter = new TextExporter(security, arg, isCancelled, fileName, wnd.TxtTemplate, wnd.TxtHeader);
					break;
				case ExportTypes.Sql:
					fileName = null;
					exporter = new DatabaseExporter(security, arg, isCancelled, (DatabaseConnectionPair)path) { CheckUnique = false };
					break;
				case ExportTypes.Bin:
					var drive = (IMarketDataDrive)path;
					fileName = drive is LocalMarketDataDrive ? drive.Path : null;
					exporter = new BinExporter(security, arg, isCancelled, drive);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			CreateWorker(values.Count, fileName);

			_worker.DoWork += (s, e) =>
			{
				_worker.ReportProgress(currProgress);

				exporter.Export(dataType, values);

				_worker.ReportProgress(100);
				Thread.Sleep(500);
				_worker.ReportProgress(0);
			};

			_worker.RunWorkerAsync();
		}

		public void Start(IMarketDataDrive destDrive, DateTime? startDate, DateTime? endDate, Security security, IMarketDataDrive sourceDrive, StorageFormats format, Type dataType, object arg)
		{
			CreateWorker(0, null);

			_worker.DoWork += (s, e) =>
			{
				var storageRegistry = ConfigManager.GetService<IStorageRegistry>();
				var storage = storageRegistry.GetStorage(security, dataType, arg, sourceDrive, format);

				try
				{
					var dates = storage.Dates.ToArray();

					if (dates.IsEmpty())
						return;

					var allDates = (startDate ?? dates.First()).Range((endDate ?? dates.Last()), TimeSpan.FromDays(1));

					var datesToExport = storage.Dates
						.Intersect(allDates)
						.Select(d =>
						{
							int count;

							if (dataType == typeof(ExecutionMessage))
								count = ((IMarketDataStorage<ExecutionMessage>)storage).Load(d).Count;
							else if (dataType == typeof(QuoteChangeMessage))
								count = ((IMarketDataStorage<QuoteChangeMessage>)storage).Load(d).Count;
							else if (dataType == typeof(Level1ChangeMessage))
								count = ((IMarketDataStorage<Level1ChangeMessage>)storage).Load(d).Count;
							else if (dataType.IsSubclassOf(typeof(CandleMessage)))
								count = ((IMarketDataStorage<CandleMessage>)storage).Load(d).Count;
							else
								throw new NotSupportedException(LocalizedStrings.Str2872Params.Put(dataType.Name));

							return Tuple.Create(d, count);
						})
						.ToArray();

					_worker.ReportProgress(0);

					var currentValuesCount = 0;
					var totalValuesCount = datesToExport.Select(d => d.Item2).Sum();

					if (!Directory.Exists(destDrive.Path))
						Directory.CreateDirectory(destDrive.Path);

					var dataPath = ((LocalMarketDataDrive)sourceDrive).GetSecurityPath(security.ToSecurityId());
					var fileName = LocalMarketDataDrive.CreateFileName(dataType, arg) + LocalMarketDataDrive.GetExtension(StorageFormats.Binary);

					foreach (var date in datesToExport)
					{
						var d = date.Item1.ToString("yyyy_MM_dd");
						var file = Path.Combine(dataPath, d, fileName);

						if (File.Exists(file))
						{
							if (!Directory.Exists(Path.Combine(destDrive.Path, d)))
								Directory.CreateDirectory(Path.Combine(destDrive.Path, d));

							File.Copy(file, Path.Combine(destDrive.Path, d, Path.GetFileName(file)), true);
						}

						if (date.Item2 == 0)
							continue;

						currentValuesCount += date.Item2;
						_worker.ReportProgress((int)Math.Round(currentValuesCount * 100m / totalValuesCount));
						this.GuiAsync(() => UpdateCount(currentValuesCount));
					}
				}
				finally
				{
					_worker.ReportProgress(100);
					Thread.Sleep(500);
					_worker.ReportProgress(0);	
				}
			};

			_worker.RunWorkerAsync();
		}

		public void Stop()
		{
			var w = _worker;

			if (w != null)
				w.CancelAsync();
		}

		private int _totalCount;

		private void CreateWorker(int totalCount, string fileName)
		{
			ClearStatus();

			_totalCount = totalCount;

			StopBtn.Visibility = Visibility.Visible;
			OpenFilePanel.Visibility = Visibility.Collapsed;

			_fileName = fileName;

			_mainGrid.IsEnabled = false;

			_worker = new BackgroundWorker { WorkerSupportsCancellation = true, WorkerReportsProgress = true };

			_worker.RunWorkerCompleted += (s, e) =>
			{
				if (e.Error != null)
				{
					e.Error.LogError();
					StopBtn.Visibility = Visibility.Collapsed;
					ProgressBar.Value = 0;
				}

				_mainGrid.IsEnabled = true;

				ProgressBarPanel.Visibility = Visibility.Collapsed;

				if (_fileName != null)
					OpenFilePanel.Visibility = Visibility.Visible;

				_worker = null;
			};

			_worker.ProgressChanged += (s, e) =>
			{
				ProgressBarPanel.Visibility = Visibility.Visible;
				ProgressBar.Value = e.ProgressPercentage;
			};
		}

		public void ClearStatus()
		{
			StatusText.Text = string.Empty;
			ProgressText.Text = string.Empty;

			StatusTextPanel.Visibility = Visibility.Collapsed;
			ProgressBarPanel.Visibility = Visibility.Collapsed;
		}

		public void TooManyValues()
		{
			UpdateStatus(LocalizedStrings.Str2911);
		}

		public void UpdateCount(int count)
		{
			ProgressText.Text = LocalizedStrings.Str2912Params.Put(count, _totalCount);

			StatusTextPanel.Visibility = Visibility.Collapsed;
			ProgressBarPanel.Visibility = Visibility.Visible;
		}

		public void DoesntExist()
		{
			UpdateStatus(LocalizedStrings.Str2913);
		}

		private void UpdateStatus(string status)
		{
			StatusText.Text = status;

			StatusTextPanel.Visibility = Visibility.Visible;
			ProgressBarPanel.Visibility = Visibility.Collapsed;
		}

		public void Load<T>(IEnumerableEx<T> source, Action<IEnumerable<T>> addValues, int maxValueCount, Action<T> itemLoaded = null)
		{
			CreateWorker(source.Count, null);

			_worker.DoWork += (sender, args) =>
			{
				var count = 0;

				foreach (var v in source)
				{
					if (_worker.CancellationPending)
						break;

					var value = v;

					var canContinue = this.GuiSync(() =>
					{
						addValues(new[] { value });
						count++;

						UpdateCount(count);

						if (itemLoaded != null)
							itemLoaded(value);

						if (count > maxValueCount)
						{
							TooManyValues();
							return false;
						}

						return true;
					});

					if (!canContinue)
						break;
				}

				if (count == 0)
					this.GuiSync(DoesntExist);
			};

			_worker.RunWorkerAsync();
		}

		private void StopBtn_Click(object sender, RoutedEventArgs e)
		{
			Stop();
		}

		private void OpenFileBtn_OnClick(object sender, RoutedEventArgs e)
		{
			Process.Start(_fileName);
		}
	}
}