namespace StockSharp.Xaml
{
	using System;
	using System.Windows;
	using System.Windows.Input;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The instrument search panel.
	/// </summary>
	public partial class SecurityLookupPanel : IPersistable
	{
		/// <summary>
		/// <see cref="RoutedCommand"/> for <see cref="SecurityLookupPanel.Lookup"/>.
		/// </summary>
		public static RoutedCommand SearchSecurityCommand = new RoutedCommand();

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityLookupPanel"/>.
		/// </summary>
		public SecurityLookupPanel()
		{
			InitializeComponent();

			Filter = new Security();
		}

		/// <summary>
		/// The filter for instrument search.
		/// </summary>
		private Security Filter
		{
			get { return (Security)SecurityFilterEditor.SelectedObject; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				SecurityFilterEditor.SelectedObject = value;
			}
		}

		/// <summary>
		/// The start of instrument search event.
		/// </summary>
		public event Action<Security> Lookup;

		private void ExecutedSearchSecurity(object sender, ExecutedRoutedEventArgs e)
		{
			Lookup.SafeInvoke(Filter);
		}

		private void CanExecuteSearchSecurity(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = Filter != null;// && !SecurityCodeLike.Text.IsEmpty();
		}

		private void SecurityCodeLike_OnPreviewKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter)
				return;

			Filter.Code = SecurityCodeLike.Text.Trim();

			if (Filter.Code == "*")
				Filter.Code = string.Empty;
			//else if (Filter.Code.IsEmpty())
			//	return;

			Lookup.SafeInvoke(Filter);
		}

		private void ClearFilter(object sender, RoutedEventArgs e)
		{
			Filter = new Security();
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			SecurityCodeLike.Text = storage.GetValue<string>("SecurityCodeLike");
			Filter = storage.GetValue<Security>("Filter");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("SecurityCodeLike", SecurityCodeLike.Text);
			storage.SetValue("Filter", Filter.Clone());
		}
	}
}