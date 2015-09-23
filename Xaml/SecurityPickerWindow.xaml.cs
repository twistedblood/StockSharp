namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Windows.Controls;

	using Ecng.Xaml;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The instrument selection window.
	/// </summary>
	public partial class SecurityPickerWindow
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityPicker"/>.
		/// </summary>
		public SecurityPickerWindow()
		{
			InitializeComponent();
			ShowOk = true;
		}

		/// <summary>
		/// The list items selection mode. The default is <see cref="DataGridSelectionMode.Extended"/>.
		/// </summary>
		public DataGridSelectionMode SelectionMode
		{
			get { return Picker.SelectionMode; }
			set { Picker.SelectionMode = value; }
		}

		/// <summary>
		/// The selected instrument.
		/// </summary>
		public Security SelectedSecurity
		{
			get { return Picker.SelectedSecurity; }
			set { Picker.SelectedSecurity = value; }
		}

		/// <summary>
		/// Selected instruments.
		/// </summary>
		public IList<Security> SelectedSecurities
		{
			get { return Picker.SelectedSecurities; }
		}

		/// <summary>
		/// Available instruments.
		/// </summary>
		public ISecurityList Securities
		{
			get { return Picker.Securities; }
		}

		/// <summary>
		/// The provider of information about instruments.
		/// </summary>
		public FilterableSecurityProvider SecurityProvider
		{
			get { return Picker.SecurityProvider; }
			set { Picker.SecurityProvider = value; }
		}

		/// <summary>
		/// To show the OK button. By default, the button is shown.
		/// </summary>
		public bool ShowOk
		{
			get { return OkBtn.GetVisibility(); }
			set { OkBtn.SetVisibility(value); }
		}

		private void PickerSecurityDoubleClick(Security security)
		{
			if (!ShowOk)
				return;

			SelectedSecurity = security;
			DialogResult = true;
		}

		private void PickerSecuritySelected(Security security)
		{
			OkBtn.IsEnabled = security != null;
		}
	}
}