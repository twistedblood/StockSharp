namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Windows;

	using Disk.SDK;
	using Disk.SDK.Provider;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Localization;

	partial class YandexLoginWindow
	{
		private const string _clientId = "fa16e5e894684f479fd32f7578f0d4a4";
		private const string _returnUrl = "https://oauth.yandex.ru/verification_code";

		private bool _authCompleted;

		public event EventHandler<GenericSdkEventArgs<string>> AuthCompleted;

		public YandexLoginWindow()
		{
			InitializeComponent();

			Browser.Visibility = Visibility.Hidden;

			BusyIndicator.BusyContent = LocalizedStrings.Authorization + "...";
			BusyIndicator.IsBusy = true;

			Browser.Navigated += BrowserNavigated;
		}

		private void BrowserNavigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
		{
			if (_authCompleted)
				return;

			Browser.Visibility = Visibility.Visible;
			BusyIndicator.IsBusy = false;
		}

		private void YandexLoginWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			new DiskSdkClient(string.Empty).AuthorizeAsync(new WebBrowserWrapper(Browser), _clientId, _returnUrl, CompleteCallback);
		}

		private void CompleteCallback(object sender, GenericSdkEventArgs<string> e)
		{
			_authCompleted = true;

			Browser.Visibility = Visibility.Hidden;

			BusyIndicator.BusyContent = LocalizedStrings.Str1574;
			BusyIndicator.IsBusy = true;

			Task.Factory
				.StartNew(() => AuthCompleted.SafeInvoke(this, new GenericSdkEventArgs<string>(e.Result)))
				.ContinueWith(res =>
				{
					BusyIndicator.IsBusy = false;
					Close();
				}, TaskScheduler.FromCurrentSynchronizationContext());
		}
	}

	/// <summary>
	/// The class for work with the Yandex.Disk.
	/// </summary>
	public class YandexDisk
	{
		private static string _rootPath = "/StockSharp";

		/// <summary>
		/// The directory in the Yandex.Disk where the files will be downloaded.
		/// </summary>
		public static string RootPath
		{
			get { return _rootPath; }
			set { _rootPath = value; }
		}

		/// <summary>
		/// To share a file.
		/// </summary>
		/// <param name="file">File.</param>
		/// <param name="owner">The login window owner.</param>
		/// <returns>The link to a file.</returns>
		public static string Publish(string file, Window owner)
		{
			if (file == null)
				throw new ArgumentNullException("file");

			if (!File.Exists(file))
				throw new FileNotFoundException(LocalizedStrings.Str1575, file);

			Exception error = null;
			string result = null;

			var loginWindow = new YandexLoginWindow();
			loginWindow.AuthCompleted += (s, e) =>
			{
				if (e.Error == null)
				{
					var client = new DiskSdkClient(e.Result);

					var remoteDir = RootPath;
					var remotePath = remoteDir + "/" + Path.GetFileName(file);

					try
					{
						TryCreateDirectory(client, remoteDir);
						UploadFile(client, remotePath, file);
						result = Publish(client, remotePath);
					}
					catch (Exception excp)
					{
						error = excp;
					}
				}
				else
					error = e.Error;
			};
			loginWindow.ShowModal(owner);

			if (error != null)
				error.Throw();

			return result;
		}

		/// <summary>
		/// To replace a file.
		/// </summary>
		/// <param name="file">File.</param>
		/// <param name="owner">The login window owner.</param>
		public static void Replace(string file, Window owner)
		{
			if (file == null)
				throw new ArgumentNullException("file");

			if (!File.Exists(file))
				throw new FileNotFoundException(LocalizedStrings.Str1575, file);

			Exception error = null;

			var loginWindow = new YandexLoginWindow();
			loginWindow.AuthCompleted += (s, e) =>
			{
				if (e.Error == null)
				{
					var client = new DiskSdkClient(e.Result);

					var remoteDir = RootPath;
					var remotePath = remoteDir + "/" + Path.GetFileName(file);

					try
					{
						TryCreateDirectory(client, remoteDir);
						UploadFile(client, remotePath, file);
					}
					catch (Exception excp)
					{
						error = excp;
					}
				}
				else
					error = e.Error;
			};
			loginWindow.ShowModal(owner);

			if (error != null)
				error.Throw();
		}

		private static void TryCreateDirectory(DiskSdkClient client, string path)
		{
			var sync = new SyncObject();
			var items = Enumerable.Empty<DiskItemInfo>();

			Exception error = null;

			EventHandler<GenericSdkEventArgs<IEnumerable<DiskItemInfo>>> listHandler = (s, e) =>
			{
				if (e.Error != null)
					error = e.Error;
				else
					items = e.Result;

				sync.Pulse();
			};

			client.GetListCompleted += listHandler;
			client.GetListAsync();

			sync.Wait();
			client.GetListCompleted -= listHandler;

			if (error != null)
				error.Throw();

			if (items.Any(i => i.IsDirectory && i.OriginalFullPath.TrimEnd("/") == path))
				return;

			EventHandler<SdkEventArgs> createHandler = (s, e) =>
			{
				error = e.Error;
				sync.Pulse();
			};

			client.MakeFolderCompleted += createHandler;
			client.MakeDirectoryAsync(path);

			sync.Wait();
			client.MakeFolderCompleted -= createHandler;

			if (error != null)
				error.Throw();
		}

		private static void UploadFile(DiskSdkClient client, string remotePath, string localPath)
		{
			var sync = new SyncObject();
			Exception error = null;

			client.UploadFileAsync(remotePath, File.OpenRead(localPath),
				new AsyncProgress((c, t) => { }),
				(us, ua) =>
				{
					error = ua.Error;
					sync.Pulse();
				});

			sync.Wait();

			if (error != null)
				error.Throw();
		}

		private static string Publish(DiskSdkClient client, string remotePath)
		{
			var sync = new SyncObject();

			Exception error = null;
			string result = null;

			EventHandler<GenericSdkEventArgs<string>> handler = (s, e) =>
			{
				if (e.Error == null)
					result = e.Result;
				else
					error = e.Error;

				sync.Pulse();
			};

			client.PublishCompleted += handler;
			client.PublishAsync(remotePath);

			sync.Wait();
			client.PublishCompleted -= handler;

			if (error != null)
				error.Throw();

			return result;
		}
	}
}
