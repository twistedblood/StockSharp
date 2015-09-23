namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The table displaying the orders log (<see cref="OrderLogItem"/>).
	/// </summary>
	public partial class OrderLogGrid
	{
		private readonly ThreadSafeObservableCollection<OrderLogItem> _items;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderLogGrid"/>.
		/// </summary>
		public OrderLogGrid()
		{
			InitializeComponent();

			var itemsSource = new ObservableCollectionEx<OrderLogItem>();
			ItemsSource = itemsSource;

			_items = new ThreadSafeObservableCollection<OrderLogItem>(itemsSource) { MaxCount = 100000 };
		}

		/// <summary>
		/// The maximum number of rows to display. The -1 value means an unlimited amount. The default value is 100000.
		/// </summary>
		public int MaxCount
		{
			get { return _items.MaxCount; }
			set { _items.MaxCount = value; }
		}

		/// <summary>
		/// Order log.
		/// </summary>
		public IListEx<OrderLogItem> LogItems
		{
			get { return _items; }
		}

		/// <summary>
		/// The selected row.
		/// </summary>
		public OrderLogItem SelectedLogItem
		{
			get { return SelectedLogItems.FirstOrDefault(); }
		}

		/// <summary>
		/// Selected rows.
		/// </summary>
		public IEnumerable<OrderLogItem> SelectedLogItems
		{
			get { return SelectedItems.Cast<OrderLogItem>(); }
		}
	}
}