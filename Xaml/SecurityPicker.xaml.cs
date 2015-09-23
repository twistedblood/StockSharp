namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The visual component for instrument searching and selection.
	/// </summary>
	public partial class SecurityPicker : IPersistable
	{
		private const DataGridSelectionMode _defaultSelectionMode = DataGridSelectionMode.Extended;

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="SecurityPicker.SelectionMode"/>.
		/// </summary>
		public static readonly DependencyProperty SelectionModeProperty = DependencyProperty.Register("SelectionMode", typeof(DataGridSelectionMode), typeof(SecurityPicker), new PropertyMetadata(_defaultSelectionMode, OnSelectionModePropertyChanged));

		private static void OnSelectionModePropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
		{
			var picker = (SecurityPicker)s;
			picker.SecuritiesCtrl.SelectionMode = (DataGridSelectionMode)e.NewValue;
		}

		/// <summary>
		/// The list items selection mode. The default is <see cref="DataGridSelectionMode.Extended"/>.
		/// </summary>
		public DataGridSelectionMode SelectionMode
		{
			get { return (DataGridSelectionMode)GetValue(SelectionModeProperty); }
			set { SetValue(SelectionModeProperty, value); }
		}

		private static void ShowCommonColumnsPropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e, HashSet<string> columns)
		{
			var picker = (SecurityPicker)s;
			var visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;

			picker.SecuritiesCtrl
				.Columns
				.Where(c => columns.Contains(c.SortMemberPath))
				.ForEach(c => c.Visibility = visibility);
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="SecurityPicker.ShowCommonStatColumns"/>.
		/// </summary>
		public static readonly DependencyProperty ShowCommonStatColumnsProperty = DependencyProperty.Register("ShowCommonStatColumns", typeof(bool), typeof(SecurityPicker), new PropertyMetadata(false, ShowCommonStatColumnsPropertyChanged));

		private static readonly HashSet<string> _commonStatColumns = new HashSet<string>
		{
			"PriceStep", "VolumeStep",
			"BestBid.Price", "BestBid.Volume",
			"BestAsk.Price", "BestAsk.Volume",
			"LastTrade.Price", "LastTrade.Volume",
			"LastTrade.Time.TimeOfDay", "LastTrade.Time.Date"
		};

		private static void ShowCommonStatColumnsPropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
		{
			ShowCommonColumnsPropertyChanged(s, e, _commonStatColumns);
		}

		/// <summary>
		/// To show main columns with statistical data.
		/// </summary>
		public bool ShowCommonStatColumns
		{
			get { return (bool)GetValue(ShowCommonStatColumnsProperty); }
			set { SetValue(ShowCommonStatColumnsProperty, value); }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="SecurityPicker.ShowCommonOptionColumns"/>.
		/// </summary>
		public static readonly DependencyProperty ShowCommonOptionColumnsProperty = DependencyProperty.Register("ShowCommonOptionColumns", typeof(bool), typeof(SecurityPicker), new PropertyMetadata(false, ShowCommonOptionColumnsPropertyChanged));

		private static readonly HashSet<string> _commonOptionColumns = new HashSet<string>
		{
			"Strike", "OptionType",
			"TheorPrice", "ImpliedVolatility",
			"HistoricalVolatility"
		};

		private static void ShowCommonOptionColumnsPropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
		{
			ShowCommonColumnsPropertyChanged(s, e, _commonOptionColumns);
		}

		/// <summary>
		/// To show main columns with statistical data.
		/// </summary>
		public bool ShowCommonOptionColumns
		{
			get { return (bool)GetValue(ShowCommonOptionColumnsProperty); }
			set { SetValue(ShowCommonOptionColumnsProperty, value); }
		}

		private readonly CachedSynchronizedSet<Security> _excludeSecurities;
		private bool _isDirty;
		private readonly string _counterMask;
		private bool _isCounterDirty;
		private string _prevFilter;
		private SecurityTypes? _prevType;

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityPicker"/>.
		/// </summary>
		public SecurityPicker()
		{
			InitializeComponent();

			_counterMask = Counter.Text;

			GuiDispatcher.GlobalDispatcher.AddPeriodicalAction(() =>
			{
				if (!_isDirty)
					return;

				_isDirty = false;
				FilterSecurities();
			});

			GuiDispatcher.GlobalDispatcher.AddPeriodicalAction(() =>
			{
				if (!_isCounterDirty)
					return;

				_isCounterDirty = false;
				UpdateCounter();
			});

			_excludeSecurities = new CachedSynchronizedSet<Security>();
			_excludeSecurities.Added += s => SecurityProviderOnSecuritiesChanged(true, NotifyCollectionChangedAction.Add, s);
			_excludeSecurities.Removed += s => SecurityProviderOnSecuritiesChanged(true, NotifyCollectionChangedAction.Remove, s);
			_excludeSecurities.Cleared += () => SecurityProviderOnSecuritiesChanged(true, NotifyCollectionChangedAction.Reset, null);

			SecurityProvider = new FilterableSecurityProvider();

			SecuritiesCtrl.SelectionMode = _defaultSelectionMode;

			UpdateCounter();

			SecurityTypeCtrl.NullItem.Description = LocalizedStrings.Str1569;
		}

		/// <summary>
		/// Events of double-clicking the mouse on the selected instrument.
		/// </summary>
		public event Action<Security> SecurityDoubleClick;

		/// <summary>
		/// The selected instrument change events.
		/// </summary>
		public event Action<Security> SecuritySelected;

		/// <summary>
		/// The table change event.
		/// </summary>
		public event Action GridChanged;

		/// <summary>
		/// The selected instrument.
		/// </summary>
		public Security SelectedSecurity
		{
			get { return SecuritiesCtrl.SelectedSecurity; }
			set { SecuritiesCtrl.SelectedSecurity = value; }
		}

		/// <summary>
		/// Selected instruments.
		/// </summary>
		public IList<Security> SelectedSecurities
		{
			get { return SecuritiesCtrl.SelectedSecurities; }
		}

		/// <summary>
		/// Instruments filtered.
		/// </summary>
		public IListEx<Security> FilteredSecurities
		{
			get { return SecuritiesCtrl.Securities; }
		}

		private SecurityTypes? _selectedType;

		/// <summary>
		/// The selected instrument type.
		/// </summary>
		public SecurityTypes? SelectedType
		{
			get { return _selectedType; }
			set { SecurityTypeCtrl.SelectedType = value; }
		}

		private string _securityFilter = string.Empty;

		/// <summary>
		/// The current filter by the instrument.
		/// </summary>
		public string SecurityFilter
		{
			get { return _securityFilter; }
			set { SecurityFilterCtrl.Text = value; }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="SecurityPicker.Title"/>.
		/// </summary>
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(SecurityPicker),
			new PropertyMetadata(string.Empty, (d, e) =>
			{
				var ctrl = d.FindLogicalChild<SecurityPicker>();
				var title = (string)e.NewValue;

				ctrl.TitleCtrl.Content = title;
				ctrl._title = title;
			}));

		private string _title = string.Empty;

		/// <summary>
		/// The title for a table. By default, it is empty.
		/// </summary>
		public string Title
		{
			get { return _title; }
			set { SetValue(TitleProperty, value); }
		}

		/// <summary>
		/// Available instruments.
		/// </summary>
		public ISecurityList Securities
		{
			get { return SecurityProvider.Securities; }
		}

		/// <summary>
		/// Instruments that should be hidden.
		/// </summary>
		public ISet<Security> ExcludeSecurities
		{
			get { return _excludeSecurities; }
		}

		private FilterableSecurityProvider _securityProvider;

		/// <summary>
		/// The provider of information about instruments.
		/// </summary>
		public FilterableSecurityProvider SecurityProvider
		{
			get { return _securityProvider; }
			set
			{
				if (_securityProvider == value)
					return;

				if (_securityProvider != null)
				{
					_securityProvider.SecuritiesChanged -= SecurityProviderOnSecuritiesChanged;
					_securityProvider.Dispose();
				}

				_securityProvider = value;

				if (_securityProvider != null)
					_securityProvider.SecuritiesChanged += SecurityProviderOnSecuritiesChanged;
				else
					_securityProvider = new FilterableSecurityProvider();

				FilterSecurities();
			}
		}

		/// <summary>
		/// The market data provider.
		/// </summary>
		public IMarketDataProvider MarketDataProvider
		{
			get { return SecuritiesCtrl.MarketDataProvider; }
			set { SecuritiesCtrl.MarketDataProvider = value; }
		}

		/// <summary>
		/// To set the visibility for a column of the table.
		/// </summary>
		/// <param name="name">The field name.</param>
		/// <param name="visibility">The visibility.</param>
		public void SetColumnVisibility(string name, Visibility visibility)
		{
			SecuritiesCtrl
				.Columns
				.Where(c => c.SortMemberPath.CompareIgnoreCase(name))
				.ForEach(c => c.Visibility = visibility);
		}

		/// <summary>
		/// To add a menu item for the table.
		/// </summary>
		/// <param name="menuItem">The menu item.</param>
		public void AddContextMenuItem(Control menuItem)
		{
			if (menuItem == null)
				throw new ArgumentNullException("menuItem");

			SecuritiesCtrl.ContextMenu.Items.Add(menuItem);
		}

		private void SecurityProviderOnSecuritiesChanged(NotifyCollectionChangedAction action, Security security)
		{
			SecurityProviderOnSecuritiesChanged(false, action, security);
		}

		private void SecurityProviderOnSecuritiesChanged(bool exclude, NotifyCollectionChangedAction action, Security security)
		{
			if (!CheckAccess())
			{
				_prevFilter = null;
				_prevType = null;
				_isDirty = true;
				return;
			}

			switch (action)
			{
				case NotifyCollectionChangedAction.Add:
				{
					if (exclude)
						FilterSecurities();
					else
					{
						if (CheckCondition(security))
							FilteredSecurities.Add(security);
					}

					break;
				}

				case NotifyCollectionChangedAction.Remove:
				{
					if (exclude)
						FilterSecurities(true);
					else
						FilteredSecurities.Remove(security);

					break;
				}

				case NotifyCollectionChangedAction.Reset:
				{
					if (exclude)
						FilterSecurities();
					else
						FilteredSecurities.Clear();

					break;
				}

				default:
					throw new ArgumentOutOfRangeException("action");
			}

			UpdateCounter();
		}

		private bool CheckCondition(Security sec)
		{
			var filter = SecurityFilter;
			var secType = SelectedType;

			return !_excludeSecurities.Contains(sec) &&
				(secType == null || sec.Type == secType) &&
					(filter.IsEmpty() ||
						(!sec.Code.IsEmpty() && sec.Code.ContainsIgnoreCase(filter)) ||
						(!sec.Name.IsEmpty() && sec.Name.ContainsIgnoreCase(filter)) ||
						(!sec.ShortName.IsEmpty() && sec.ShortName.ContainsIgnoreCase(filter)) ||
						sec.Id.ContainsIgnoreCase(filter));
		}

		private void FilterSecurities(bool fullRefresh = false)
		{
			var filter = SecurityFilter;

			if (filter != null)
				filter = filter.Trim();

			var secType = SelectedType;

			// при уточняющем фильтре выполняем поиск в найденных инструментах
			if (!fullRefresh
				&& !_prevFilter.IsEmpty() && filter.StartsWith(_prevFilter, StringComparison.InvariantCultureIgnoreCase)
				&& (_prevType == secType || _prevType == null && secType != null)
				&& FilteredSecurities.Count < 500)
			{
				FilteredSecurities.RemoveWhere(s => !CheckCondition(s));
				UpdateCounter();
				return;
			}

			// LookupByCode не принимает пустой фильтр
			if (filter.IsEmpty())
				filter = "*";

			var securities = SecurityProvider.LookupByCode(filter);

			var toAdd = securities
				.Where(s => !_excludeSecurities.Contains(s) && (secType == null || s.Type == secType))
				.ToArray();

			if (!FilteredSecurities.SequenceEqual(toAdd))
			{
				FilteredSecurities.Clear();
				FilteredSecurities.AddRange(toAdd);
			}

			UpdateCounter();
		}

		private void UpdateCounter()
		{
			Counter.Text = _counterMask.Put(FilteredSecurities.Count,
				Securities.Count - SecurityProvider.ExcludedCount - _excludeSecurities.Count);
		}

		private void HandleDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var security = SecuritiesCtrl.CurrentSecurity;

			if (security == null)
				return;

			SecurityDoubleClick.SafeInvoke(security);
			e.Handled = true;
		}

		private void SecurityTypes_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			_prevType = _selectedType;
			_selectedType = SecurityTypeCtrl.SelectedType;
			FilterSecurities();
			_prevType = _selectedType;

			GridChanged.SafeInvoke();
		}

		private void SecurityFilterCtrl_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			_prevFilter = _securityFilter;
			_securityFilter = SecurityFilterCtrl.Text;
			FilterSecurities();
			_prevFilter = _securityFilter;

			GridChanged.SafeInvoke();
		}

		private void SecuritiesCtrl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SecuritySelected.SafeInvoke(SecuritiesCtrl.SelectedSecurity);
		}

		private void SecuritiesCtrl_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			GridChanged.SafeInvoke();
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			var gridSettings = storage.GetValue<SettingsStorage>("GridSettings");

			if (gridSettings != null)
				SecuritiesCtrl.Load(gridSettings);

			SecurityFilter = storage.GetValue<string>("SecurityFilter");
			SelectedType = storage.GetValue<string>("SecurityType").To<SecurityTypes?>();
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("GridSettings", SecuritiesCtrl.Save());
			storage.SetValue("SecurityFilter", SecurityFilter);
			storage.SetValue("SecurityType", SelectedType.To<string>());
		}
	}
}