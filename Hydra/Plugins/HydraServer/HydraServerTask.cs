namespace StockSharp.Hydra.HydraServer
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Linq;
	using System.Security;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.History;
	using StockSharp.Algo.History.Hydra;
	using StockSharp.Algo.Storages;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[TaskDoc("http://stocksharp.com/doc/html/c84d96f5-d466-4dbd-b7d4-9f87cba8ea7f.htm")]
	[TaskIcon("hydra_server_logo.png")]
	[TaskCategory(TaskCategories.History | TaskCategories.Ticks | TaskCategories.Stock |
		TaskCategories.Forex | TaskCategories.Free | TaskCategories.MarketDepth | TaskCategories.OrderLog |
		TaskCategories.Level1 | TaskCategories.Candles | TaskCategories.Transactions)]
	class HydraServerTask : BaseHydraTask, ISecurityDownloader
	{
		private const string _sourceName = "S#.Data Server";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class HydraServerSettings : HydraTaskSettings
		{
			public HydraServerSettings(HydraTaskSettings settings)
				: base(settings)
			{
				ExtensionInfo.TryAdd("IgnoreWeekends", true);
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.AddressKey, true)]
			[PropertyOrder(0)]
			public Uri Address
			{
				get { return ExtensionInfo["Address"].To<Uri>(); }
				set { ExtensionInfo["Address"] = value.ToString(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.Str2302Key)]
			[PropertyOrder(1)]
			public string Login
			{
				get { return (string)ExtensionInfo["Login"]; }
				set { ExtensionInfo["Login"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.Str2303Key)]
			[PropertyOrder(2)]
			public SecureString Password
			{
				get { return ExtensionInfo["Password"].To<SecureString>(); }
				set { ExtensionInfo["Password"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2282Key)]
			[DescriptionLoc(LocalizedStrings.Str2304Key)]
			[PropertyOrder(3)]
			public DateTime StartFrom
			{
				get { return ExtensionInfo["StartFrom"].To<DateTime>(); }
				set { ExtensionInfo["StartFrom"] = value.Ticks; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2284Key)]
			[DescriptionLoc(LocalizedStrings.Str2285Key)]
			[PropertyOrder(4)]
			public int Offset
			{
				get { return ExtensionInfo["HydraOffset"].To<int>(); }
				set { ExtensionInfo["HydraOffset"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2286Key)]
			[DescriptionLoc(LocalizedStrings.Str2287Key)]
			[PropertyOrder(5)]
			public bool IgnoreWeekends
			{
				get { return (bool)ExtensionInfo["IgnoreWeekends"]; }
				set { ExtensionInfo["IgnoreWeekends"] = value; }
			}

			[Browsable(false)]
			public override IEnumerable<Level1Fields> SupportedLevel1Fields
			{
				get { return base.SupportedLevel1Fields; }
				set { base.SupportedLevel1Fields = value; }
			}
		}

		private HydraServerSettings _settings;

		public HydraServerTask()
		{
			_supportedCandleSeries = new[]
			{
				TimeSpan.FromMinutes(1),
				TimeSpan.FromMinutes(5),
				TimeSpan.FromMinutes(15),
				TimeSpan.FromHours(1),
				TimeSpan.FromDays(1)
			}
			.Select(tf => new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = tf
			}).ToArray();
		}

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new HydraServerSettings(settings);

			if (settings.IsDefault)
			{
				_settings.Offset = 1;
				_settings.StartFrom = new DateTime(2000,1,1);
				_settings.Address = "net.tcp://localhost:8000".To<Uri>();
				_settings.Login = string.Empty;
				_settings.Password = new SecureString();
				_settings.Interval = TimeSpan.FromDays(1);
				_settings.IgnoreWeekends = true;
			}
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		private readonly Type[] _supportedMarketDataTypes =
		{
			typeof(Trade),
			typeof(MarketDepth),
			typeof(OrderLogItem),
			typeof(Level1ChangeMessage),
			typeof(Candle)
		};

		private readonly IEnumerable<CandleSeries> _supportedCandleSeries;

		public override IEnumerable<CandleSeries> SupportedCandleSeries
		{
			get { return _supportedCandleSeries; }
		}

		public override IEnumerable<Type> SupportedMarketDataTypes
		{
			get { return _supportedMarketDataTypes; }
		}

		protected override TimeSpan OnProcess()
		{
			using (var client = CreateClient())
			{
				var allSecurity = this.GetAllSecurity();

				if (allSecurity != null)
				{
					this.AddInfoLog(LocalizedStrings.Str2305);
					client.Refresh(EntityRegistry.Securities, new Security(), SaveSecurity, () => !CanProcess(false));
				}

				var supportedDataTypes = Enumerable.Empty<Type>();

				if (allSecurity != null)
					supportedDataTypes = SupportedMarketDataTypes.Intersect(allSecurity.MarketDataTypes).ToArray();

				this.AddInfoLog(LocalizedStrings.Str2306Params.Put(_settings.StartFrom));

				var hasSecurities = false;

				foreach (var security in GetWorkingSecurities())
				{
					hasSecurities = true;

					if (!CanProcess())
						break;

					this.AddInfoLog(LocalizedStrings.Str2307Params.Put(security.Security.Id));

					//foreach (var dataType in security.GetSupportDataTypes(this))
					foreach (var dataType in (allSecurity == null ? security.MarketDataTypes : supportedDataTypes))
					{
						if (!CanProcess())
							break;

						if (dataType == typeof(Candle))
						{
							foreach (var series in (allSecurity ?? security).CandleSeries)
							{
								if (!DownloadData(security, typeof(TimeFrameCandle), series.Arg, client))
									break;
							}
						}
						else
						{
							if (!DownloadData(security, dataType, null, client))
								break;	
						}
					}
				}

				if (!hasSecurities)
				{
					this.AddWarningLog(LocalizedStrings.Str2292);
					return TimeSpan.MaxValue;
				}

				if (CanProcess())
					this.AddInfoLog(LocalizedStrings.Str2300);

				return base.OnProcess();	
			}
		}

		private bool DownloadData(HydraTaskSecurity security, Type dataType, object arg, RemoteStorageClient client)
		{
			var localStorage = StorageRegistry.GetStorage(security.Security, dataType, arg, _settings.Drive, _settings.StorageFormat);

			var remoteStorage = client.GetRemoteStorage(security.Security.ToSecurityId(), dataType, arg, _settings.StorageFormat);

			var endDate = DateTime.Today - TimeSpan.FromDays(_settings.Offset);
			var dates = remoteStorage.Dates.Where(date => date >= _settings.StartFrom && date <= endDate).Except(localStorage.Dates).ToArray();

			if (dates.IsEmpty())
			{
				if (!CanProcess())
					return false;
			}
			else
			{
				this.AddInfoLog(LocalizedStrings.Str2308Params.Put(dataType.Name));

				foreach (var date in dates)
				{
					if (!CanProcess())
						return false;

					if (_settings.IgnoreWeekends && !security.IsTradeDate(date))
					{
						this.AddDebugLog(LocalizedStrings.WeekEndDate, date);
						continue;
					}

					this.AddDebugLog(LocalizedStrings.StartDownloding, dataType, arg, date, security.Security.Id);

					using (var stream = remoteStorage.LoadStream(date))
					{
						if (stream == Stream.Null)
						{
							this.AddDebugLog(LocalizedStrings.NoData);
							continue;
						}

						this.AddInfoLog(LocalizedStrings.Str2309Params.Put(date));

						localStorage.Drive.SaveStream(date, stream);

						var info = localStorage.Serializer.CreateMetaInfo(date);

						stream.Position = 0;
						info.Read(stream);

						// TODO Remove after few releases
						if (dataType == typeof(Trade))
						{
							dataType = typeof(ExecutionMessage);
							arg = ExecutionTypes.Tick;
						}
						else if (dataType == typeof(OrderLogItem))
						{
							dataType = typeof(ExecutionMessage);
							arg = ExecutionTypes.OrderLog;
						}
						else if (dataType == typeof(MarketDepth))
						{
							dataType = typeof(QuoteChangeMessage);
						}
						else if (dataType.IsSubclassOf(typeof(Candle)))
						{
							dataType = dataType.ToCandleMessageType();
						}

						RaiseDataLoaded(security.Security, dataType, arg, date, info.Count);
					}
				}
			}

			return true;
		}

		void ISecurityDownloader.Refresh(ISecurityStorage storage, Security criteria, Action<Security> newSecurity, Func<bool> isCancelled)
		{
			using (var client = CreateClient())
				client.Refresh(storage, criteria, newSecurity, isCancelled);
		}

		private RemoteStorageClient CreateClient()
		{
			return new RemoteStorageClient(_settings.Address)
			{
				Credentials =
				{
					Login = _settings.Login,
					Password = _settings.Password
				}
			};
		}
	}
}