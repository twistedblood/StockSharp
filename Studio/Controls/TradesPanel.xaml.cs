namespace StockSharp.Studio.Controls
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Collections;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.Str3278Key)]
	[DescriptionLoc(LocalizedStrings.Str3279Key)]
	[Icon("images/deal_24x24.png")]
	public partial class TradesPanel
	{
		private decimal? _volumeFilter;
		private readonly SynchronizedSet<string> _securityIds = new SynchronizedSet<string>(StringComparer.InvariantCultureIgnoreCase);

		private Security[] Securities
		{
			get
			{
				var entityRegistry = ConfigManager.GetService<IStudioEntityRegistry>();

				return _securityIds
					.Select(id => entityRegistry.Securities.ReadById(id))
					.Where(s => s != null)
					.ToArray();
			}
		}

		public TradesPanel()
		{
			InitializeComponent();

			TradesGrid.PropertyChanged += (s, e) => RaiseChangedCommand();
			TradesGrid.SelectionChanged += (s, e) => RaiseSelectedCommand();

			AlertBtn.SchemaChanged += RaiseChangedCommand;

			GotFocus += (s, e) => RaiseSelectedCommand();

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.Register<ResetedCommand>(this, false, cmd =>
			{
				TradesGrid.Trades.Clear();
				Securities.ForEach(s => new RequestMarketDataCommand(s, MarketDataTypes.Trades).Process(this));
			});
			cmdSvc.Register<NewTradesCommand>(this, false, cmd => AddTrades(cmd.Trades));
			cmdSvc.Register<BindStrategyCommand>(this, false, cmd =>
			{
				if (!cmd.CheckControl(this))
					return;

				var selectedSecurities = Securities;

				if (!selectedSecurities.Any())
					return;

				selectedSecurities.ForEach(s => new RequestMarketDataCommand(s, MarketDataTypes.Trades).Process(this));
				new RequestTradesCommand().Process(this);
			});

			WhenLoaded(() => new RequestBindSource(this).SyncProcess(this));
		}

		private void RaiseChangedCommand()
		{
			new ControlChangedCommand(this).Process(this);
		}

		private void RaiseSelectedCommand()
		{
			new SelectCommand<Trade>(TradesGrid.SelectedTrade, false).Process(this);
		}

		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("GridSettings", TradesGrid.Save());
			storage.SetValue("Securities", _securityIds.ToArray());
			storage.SetValue("AlertSettings", AlertBtn.Save());
			storage.SetValue("VolumeFilter", VolumeFilter.Value);
		}

		public override void Load(SettingsStorage storage)
		{
			TradesGrid.Load(storage.GetValue<SettingsStorage>("GridSettings"));

			_securityIds.SyncDo(list =>
			{
				list.Clear();
				list.AddRange(storage.GetValue("Securities", ArrayHelper.Empty<string>()));
			});

			var alertSettings = storage.GetValue<SettingsStorage>("AlertSettings");
			if (alertSettings != null)
				AlertBtn.Load(alertSettings);

			VolumeFilter.Value = storage.GetValue<decimal?>("VolumeFilter");
		}

		public override void Dispose()
		{
			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.UnRegister<ResetedCommand>(this);
			cmdSvc.UnRegister<NewTradesCommand>(this);

			AlertBtn.Dispose();
		}

		private void AddTrades(IEnumerable<Trade> trades)
		{
			if (!_securityIds.IsEmpty())
				trades = trades.Where(t => _securityIds.Contains(t.Security.Id));

			if (_volumeFilter != null && _volumeFilter > 0)
				trades = trades.Where(t => t.Volume >= _volumeFilter);

			TradesGrid.Trades.AddRange(trades);
			AlertBtn.Process(trades.Select(t => t.ToMessage()));
		}

		private void Filter_OnClick(object sender, RoutedEventArgs e)
		{
			var window = new SecuritiesWindowEx
			{
				SecurityProvider = ConfigManager.GetService<FilterableSecurityProvider>()
			};

			var selectedSecurities = Securities;

			window.SelectSecurities(selectedSecurities);

			if (!window.ShowModal(this))
				return;

			var toRemove = selectedSecurities.Except(window.SelectedSecurities).ToArray();
			var toAdd = window.SelectedSecurities.Except(selectedSecurities).ToArray();

			_securityIds.SyncDo(list =>
			{
				list.Clear();
				list.AddRange(window.SelectedSecurities.Select(s => s.Id));
			});

			toRemove.ForEach(s => new RefuseMarketDataCommand(s, MarketDataTypes.Trades).Process(this));
			toAdd.ForEach(s => new RequestMarketDataCommand(s, MarketDataTypes.Trades).Process(this));

			TradesGrid.Trades.Clear();

			new RequestTradesCommand().Process(this);
			new ControlChangedCommand(this).Process(this);
		}

		private void VolumeFilter_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			_volumeFilter = e.NewValue as decimal?;
		}
	}
}