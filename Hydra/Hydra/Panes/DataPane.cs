namespace StockSharp.Hydra.Panes
{
	using System;
	using System.ComponentModel;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Controls;
	using StockSharp.Hydra.Core;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit;

	public abstract class DataPane : UserControl, IPane, INotifyPropertyChanged
	{
		public abstract Security SelectedSecurity { get; set; }

		protected abstract Type DataType { get; }

		protected virtual object Arg { get { return null; } }

		protected IStorageRegistry StorageRegistry
		{
			get { return ConfigManager.GetService<IStorageRegistry>(); }
		}

		public abstract string Title { get; }

		protected void UpdateTitle()
		{
			_propertyChanged.SafeInvoke(this, "Title");
		}

		Uri IPane.Icon
		{
			get { return null; }
		}

		public virtual bool InProcess
		{
			get { return Progress.IsStarted; }
		}

		public virtual bool IsValid
		{
			get { return true; }
		}

		protected DateTime? From
		{
			get
			{
				var value = _from.Value;
				return value == null ? (DateTime?)null : value.Value.ChangeKind(DateTimeKind.Utc);
			}
			set { _from.Value = value; }
		}

		protected DateTime? To
		{
			get
			{
				var value = _to.Value;
				return value == null ? (DateTime?)null : value.Value.ChangeKind(DateTimeKind.Utc);
			}
			set { _to.Value = value; }
		}

		protected IMarketDataDrive Drive
		{
			get { return _drivePanel.SelectedDrive; }
			set { _drivePanel.SelectedDrive = value; }
		}

		protected StorageFormats StorageFormat
		{
			get { return _drivePanel.StorageFormat; }
			set { _drivePanel.StorageFormat = value; }
		}

		private ExportProgress Progress
		{
			get { return ((dynamic)this).Progress; }
		}

		private ExportButton _exportBtn;
		private Func<IEnumerableEx> _getItems;
		private DateTimePicker _from;
		private DateTimePicker _to;
		private DrivePanel _drivePanel;

		protected void Init(ExportButton exportBtn, Grid mainGrid, Func<IEnumerableEx> getItems)
		{
			if (exportBtn == null)
				throw new ArgumentNullException("exportBtn");

			if (mainGrid == null)
				throw new ArgumentNullException("mainGrid");

			if (getItems == null)
				throw new ArgumentNullException("getItems");

			_exportBtn = exportBtn;
			_exportBtn.ExportStarted += ExportBtnOnExportStarted;

			dynamic ctrl = this;

			_from = ctrl.FromCtrl;
			_to = ctrl.ToCtrl;
			_drivePanel = ctrl.DrivePanel;

			Progress.Init(_exportBtn, mainGrid);

			From = DateTime.Today - TimeSpan.FromDays(7);
			To = DateTime.Today + TimeSpan.FromDays(1);

			_getItems = getItems;
		}

		protected virtual bool CheckSecurity()
		{
			if (SelectedSecurity != null)
				return true;

			new MessageBoxBuilder()
				.Caption(Title)
				.Text(LocalizedStrings.Str2875)
				.Info()
				.Owner(this)
				.Show();

			return false;
		}

		protected virtual bool CanDirectBinExport
		{
			get { return _exportBtn.ExportType == ExportTypes.Bin; }
		}

		protected virtual void ExportBtnOnExportStarted()
		{
			if (!CheckSecurity())
				return;

			if (_getItems().Count == 0)
			{
				Progress.DoesntExist();
				return;
			}

			var path = _exportBtn.GetPath(SelectedSecurity, DataType, Arg, From, To, Drive);

			if (path == null)
				return;

			if (CanDirectBinExport)
			{
				var destDrive = (IMarketDataDrive)path;

				if (destDrive.Path.ComparePaths(Drive.Path))
				{
					new MessageBoxBuilder()
						.Caption("S#.Data")
						.Text(LocalizedStrings.Str2876)
						.Error()
						.Owner(this)
							.Show();

					return;
				}

				Progress.Start(destDrive, From, To, SelectedSecurity, Drive, StorageFormat, DataType, Arg);
			}
			else
				Progress.Start(SelectedSecurity, DataType, Arg, _getItems(), path);
		}

		public virtual void Load(SettingsStorage storage)
		{
			if (storage.ContainsKey("SelectedSecurity"))
				SelectedSecurity = ConfigManager.GetService<IEntityRegistry>().Securities.ReadById(storage.GetValue<string>("SelectedSecurity"));

			From = storage.GetValue<DateTime?>("From");
			To = storage.GetValue<DateTime?>("To");

			if (storage.ContainsKey("Drive"))
				Drive = DriveCache.Instance.GetDrive(storage.GetValue<string>("Drive"));

			StorageFormat = storage.GetValue<StorageFormats>("StorageFormat");
		}

		public virtual void Save(SettingsStorage storage)
		{
			if (SelectedSecurity != null)
				storage.SetValue("SelectedSecurity", SelectedSecurity.Id);

			if (From != null)
				storage.SetValue("From", (DateTime)From);

			if (To != null)
				storage.SetValue("To", (DateTime)To);

			if (Drive != null)
				storage.SetValue("Drive", Drive.Path);

			storage.SetValue("StorageFormat", StorageFormat);
		}

		private PropertyChangedEventHandler _propertyChanged;

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add { _propertyChanged += value; }
			remove { _propertyChanged -= value; }
		}

		void IDisposable.Dispose()
		{
			Progress.Stop();
		}
	}
}