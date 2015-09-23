namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Threading;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Windows.Media.Imaging;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Interop;
	using Ecng.Localization;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Testing;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The panel for the new connections creating <see cref="IMessageAdapter"/>.
	/// </summary>
	public partial class MessageAdaptersPanel
	{
		private sealed class ConnectorRow : NotifiableObject
		{
			public ConnectorRow(ConnectorInfo info, IMessageAdapter adapter)
			{
				if (info == null)
					throw new ArgumentNullException("info");

				if (adapter == null)
					throw new ArgumentNullException("adapter");

				Info = info;
				Adapter = adapter;
			}

			public ConnectorInfo Info { get; private set; }

			public IMessageAdapter Adapter { get; private set; }

			private bool _isEnabled = true;

			public bool IsEnabled
			{
				get { return _isEnabled; }
				set
				{
					_isEnabled = value;
					NotifyChanged("IsEnabled");
				}
			}

			public string Description
			{
				get { return Adapter.ToString(); }
			}

			public void Refresh()
			{
				NotifyChanged("Description");
				NotifyChanged("IsTransactionEnabled");
				NotifyChanged("IsMarketDataEnabled");
			}
		}

		private sealed class ConnectorInfoList : BaseList<ConnectorInfo>
		{
			private readonly MessageAdaptersPanel _parent;
			private readonly Languages _language;
			private readonly Dictionary<ConnectorInfo, MenuItem> _items = new Dictionary<ConnectorInfo, MenuItem>();

			public ConnectorInfoList(MessageAdaptersPanel parent)
			{
				if (parent == null)
					throw new ArgumentNullException("parent");

				_parent = parent;
				_language = Thread.CurrentThread.CurrentCulture.Name == "en-US" ? Languages.English : Languages.Russian;
			}

			private ItemCollection Items
			{
				get { return _parent.ConnectionsMenu.Items; }
			}

			protected override bool OnAdding(ConnectorInfo item)
			{
				var mi = new MenuItem
				{
					Header = item.Name,
					ToolTip = item.Description
				};

				mi.Click += (sender, args) =>
				{
					// TODO
					var adapter = item.TransactionAdapterType.CreateInstanceArgs<IMessageAdapter>(new object[] { _parent.Adapter.TransactionIdGenerator });

					var wnd = new MessageAdapterWindow
					{
						Adapter = adapter,
					};

					if (!wnd.ShowModal(_parent))
						return;

					var row = new ConnectorRow(item, wnd.Adapter);

					_parent.Adapter.InnerAdapters[wnd.Adapter] = 0;

					_parent._connectorRows.Add(row);
					_parent.ConnectorsChanged.SafeInvoke();
				};

				_items.Add(item, mi);

				var separator = Items
					.OfType<HeaderedSeparator>()
					.FirstOrDefault(s => s.Header == item.Category);

				if (separator == null)
				{
					var key = item.PreferLanguage == _language ? -1 : (int)item.PreferLanguage;

					separator = new HeaderedSeparator
					{
						Header = item.Category,
						Tag = key
					};

					var prev = Items.OfType<HeaderedSeparator>().FirstOrDefault();
					if (prev != null && key >= (int)prev.Tag)
					{
						var index = Items
							.OfType<Control>()
							.Select((i, idx) => new { Item = i, Index = idx })
							.FirstOrDefault(p => p.Item.Tag != null && (int)p.Item.Tag > key);

						if (index == null)
							Items.Add(separator);
						else
							Items.Insert(index.Index, separator);
					}
					else
						Items.Insert(0, separator);
				}

				var categoryIndex = Items.IndexOf(separator);

				var categoryItems = Items
					.Cast<object>()
					.Skip(categoryIndex + 1)
					.TakeWhile(o => !(o is HeaderedSeparator))
					.ToArray();

				Items.Insert(categoryIndex + categoryItems.Length + 1, mi);

				return base.OnAdding(item);
			}

			protected override bool OnRemoving(ConnectorInfo item)
			{
				Items.Remove(_items[item]);
				_items.Remove(item);
				return base.OnRemoving(item);
			}

			protected override bool OnClearing()
			{
				Items.Clear();
				_items.Clear();
				return base.OnClearing();
			}
		}

		/// <summary>
		/// <see cref="RoutedCommand"/> for the connection removal.
		/// </summary>
		public static readonly RoutedCommand RemoveSessionCommand = new RoutedCommand();

		/// <summary>
		/// <see cref="RoutedCommand"/> to enable connection.
		/// </summary>
		public static readonly RoutedCommand EnableSessionCommand = new RoutedCommand();

		private readonly ObservableCollection<ConnectorRow> _connectorRows = new ObservableCollection<ConnectorRow>();

		/// <summary>
		/// Initializes a new instance of the <see cref="MessageAdaptersPanel"/>.
		/// </summary>
		public MessageAdaptersPanel()
		{
			InitializeComponent();

			ConnectorsGrid.ItemsSource = _connectorRows;
			_connectorsInfo = new ConnectorInfoList(this);
		}

		/// <summary>
		/// Auto connect.
		/// </summary>
		public bool AutoConnect
		{
			get { return AutoConnectCtrl.IsChecked == true; }
			set { AutoConnectCtrl.IsChecked = value; }
		}

		private BasketMessageAdapter _adapter;

		/// <summary>
		/// Adapter aggregator.
		/// </summary>
		public BasketMessageAdapter Adapter
		{
			get { return _adapter; }
			set
			{
				_adapter = value;

				_connectorRows.Clear();

				if (_adapter == null)
					return;

				// TODO добавить панель настроек для эмуляционной сессии
				var adapters = _adapter.InnerAdapters.Where(s => !(s is HistoryMessageAdapter));

				_connectorRows.AddRange(adapters.Select(a => new ConnectorRow(GetInfo(a), a) { IsEnabled = _adapter.InnerAdapters[a] != -1 }));
			}
		}

		private readonly IList<ConnectorInfo> _connectorsInfo;

		/// <summary>
		/// Visual description of available connections.
		/// </summary>
		public IList<ConnectorInfo> ConnectorsInfo
		{
			get { return _connectorsInfo; }
		}

		/// <summary>
		/// The settings change event.
		/// </summary>
		public event Action ConnectorsChanged;

		/// <summary>
		/// The connection status check event.
		/// </summary>
		public event Func<ConnectionStates> CheckConnectionState;

		private ConnectorRow SelectedInfo
		{
			get { return ConnectorsGrid != null ? (ConnectorRow)ConnectorsGrid.SelectedItem : null; }
		}

		private ConnectorInfo GetInfo(IMessageAdapter adapter)
		{
			if (adapter == null)
				throw new ArgumentNullException("adapter");

			var info = ConnectorsInfo.FirstOrDefault(i => i.TransactionAdapterType.IsInstanceOfType(adapter) || i.MarketDataAdapterType.IsInstanceOfType(adapter));

			if (info == null)
				throw new ArgumentException(LocalizedStrings.Str1553Params.Put(adapter.GetType()), "adapter");

			return info;
		}

		private bool CheckConnected(string message)
		{
			if (!SelectedInfo.IsEnabled || CheckConnectionState == null)
				return true;

			var connectionState = CheckConnectionState();

			if (connectionState == ConnectionStates.Disconnected || connectionState == ConnectionStates.Failed)
				return true;

			new MessageBoxBuilder()
				.Owner(this)
				.Warning()
				.Text(message)
				.Show();

			return false;
		}

		private void ExecutedRemoveSession(object sender, ExecutedRoutedEventArgs e)
		{
			if (!CheckConnected(LocalizedStrings.Str1554))
				return;

			Adapter.InnerAdapters.Remove(SelectedInfo.Adapter);
			_connectorRows.Remove(SelectedInfo);
			ConnectorsChanged.SafeInvoke();
		}

		private void CanExecuteRemoveSession(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedInfo != null;
		}

		private void ConnectorsGrid_DoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (SelectedInfo == null)
				return;

			if (!CheckConnected(LocalizedStrings.Str1555))
				return;

			var wnd = new MessageAdapterWindow
			{
				Adapter = SelectedInfo.Adapter
			};

			if (!wnd.ShowModal(this))
				return;

			SelectedInfo.Refresh();

			ConnectorsChanged.SafeInvoke();
		}

		private void ConnectorsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ChangeDisableEnableIcon(SelectedInfo != null && SelectedInfo.IsEnabled);
		}

		private void ExecutedEnableSession(object sender, ExecutedRoutedEventArgs e)
		{
			if (!CheckConnected(LocalizedStrings.Str1556))
				return;

			SelectedInfo.IsEnabled = !SelectedInfo.IsEnabled;

			Adapter.InnerAdapters[SelectedInfo.Adapter] = SelectedInfo.IsEnabled ? 0 : -1;

			ChangeDisableEnableIcon(SelectedInfo.IsEnabled);
			ConnectorsChanged.SafeInvoke();
		}

		private void CanExecuteEnableSession(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedInfo != null;
		}

		private void ChangeDisableEnableIcon(bool isEnabled)
		{
			var bmp = EnableImg;
			bmp.Source = new BitmapImage(new Uri("pack://application:,,,/StockSharp.Xaml;component/Images/" + (isEnabled ? "disabled.png" : "enabled.png")));
			bmp.InvalidateMeasure();
			bmp.InvalidateVisual();
			bmp.SetToolTip(isEnabled ? LocalizedStrings.Str1557 : LocalizedStrings.Str1558);
		}
	}

	/// <summary>
	/// Information about connection.
	/// </summary>
	public class ConnectorInfo
	{
		/// <summary>
		/// The connection name.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// The connection description.
		/// </summary>
		public string Description { get; private set; }

		/// <summary>
		/// The connection description.
		/// </summary>
		public string Category { get; private set; }

		/// <summary>
		/// The target audience.
		/// </summary>
		public Languages PreferLanguage { get; private set; }

		/// <summary>
		/// Platform.
		/// </summary>
		public Platforms Platform { get; private set; }

		/// <summary>
		/// The type of transaction adapter.
		/// </summary>
		public Type TransactionAdapterType { get; set; }

		/// <summary>
		/// The type of market data adapter.
		/// </summary>
		public Type MarketDataAdapterType { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectorInfo"/>.
		/// </summary>
		/// <param name="transactionAdapterType">The type of transaction adapter.</param>
		/// <param name="marketDataAdapterType">The type of market data adapter.</param>
		public ConnectorInfo(Type transactionAdapterType, Type marketDataAdapterType)
		{
			if (transactionAdapterType == null && marketDataAdapterType == null)
				throw new ArgumentNullException("transactionAdapterType");

			if (transactionAdapterType != null && !typeof(IMessageAdapter).IsAssignableFrom(transactionAdapterType))
				throw new ArgumentException("transactionAdapterType");

			if (marketDataAdapterType != null && !typeof(IMessageAdapter).IsAssignableFrom(marketDataAdapterType))
				throw new ArgumentException("marketDataAdapterType");

			var adapterType = transactionAdapterType ?? marketDataAdapterType;

			Name = adapterType.GetDisplayName();
			Description = adapterType.GetDescription();
			Category = adapterType.GetCategory(LocalizedStrings.Str1559);

			var targetPlatform = adapterType.GetAttribute<TargetPlatformAttribute>();
			if (targetPlatform != null)
			{
				PreferLanguage = targetPlatform.PreferLanguage;
				Platform = targetPlatform.Platform;
			}
			else
			{
				PreferLanguage = Languages.English;
				Platform = Platforms.AnyCPU;
			}
		}
	}
}