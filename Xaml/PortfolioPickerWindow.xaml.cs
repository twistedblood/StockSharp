namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Windows.Input;

	using Ecng.Collections;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The portfolio selection window.
	/// </summary>
	partial class PortfolioPickerWindow
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioPickerWindow"/>.
		/// </summary>
		public PortfolioPickerWindow()
		{
			InitializeComponent();

			var itemsSource = new ObservableCollectionEx<Portfolio>();
			PortfoliosCtrl.ItemsSource = itemsSource;
			Portfolios = new ThreadSafeObservableCollection<Portfolio>(itemsSource);
		}

		/// <summary>
		/// The selected portfolio.
		/// </summary>
		public Portfolio SelectedPortfolio
		{
			get { return (Portfolio)PortfoliosCtrl.SelectedItem; }
			set { PortfoliosCtrl.SelectedItem = value; }
		}

		/// <summary>
		/// Available portfolios.
		/// </summary>
		public IListEx<Portfolio> Portfolios { get; private set; }

		private IConnector _connector;

		/// <summary>
		/// Connection to the trading system.
		/// </summary>
		public IConnector Connector
		{
			get { return _connector; }
			set
			{
				if (_connector == value)
					return;

				if (_connector != null)
				{
					_connector.NewPortfolios -= OnNewPortfolios;
					Portfolios.Clear();
				}

				_connector = value;

				if (_connector != null)
				{
					OnNewPortfolios(_connector.Portfolios);
					_connector.NewPortfolios += OnNewPortfolios;
				}
			}
		}

		private void OnNewPortfolios(IEnumerable<Portfolio> portfolios)
		{
			Portfolios.AddRange(portfolios);
		}

		private void PortfoliosCtrl_OnSelectionChanged(object sender, EventArgs e)
		{
			OkBtn.IsEnabled = SelectedPortfolio != null;
		}

		private void HandleDoubleClick(object sender, MouseButtonEventArgs e)
		{
			SelectedPortfolio = (Portfolio)PortfoliosCtrl.CurrentItem;
			DialogResult = true;
		}
	}
}