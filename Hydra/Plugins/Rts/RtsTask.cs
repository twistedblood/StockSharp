namespace StockSharp.Hydra.Rts
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Localization;
	using Ecng.Collections;

	using StockSharp.Algo;
	using StockSharp.Algo.History.Russian.Rts;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2822Key)]
	[TargetPlatform(Languages.Russian)]
	[TaskDoc("http://stocksharp.com/doc/html/0da19c49-1f11-455e-bbd5-c20e428e5149.htm")]
	[TaskIcon("rts_logo.png")]
	[TaskCategory(TaskCategories.Russia | TaskCategories.History |
		TaskCategories.Stock | TaskCategories.Free | TaskCategories.Ticks)]
	class RtsTask : BaseHydraTask
	{
		private const string _sourceName = "RTS";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class RtsSettings : HydraTaskSettings
		{
			private const string _rtsStndard = "RTS Standard";
			private const string _usdRur = "USD/RUR";

			public RtsSettings(HydraTaskSettings settings)
				: base(settings)
			{
				ExtensionInfo.TryAdd("UseTemporaryFiles", TempFiles.UseAndDelete.To<string>());
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2282Key)]
			[DescriptionLoc(LocalizedStrings.Str2283Key)]
			[PropertyOrder(0)]
			public DateTime StartFrom
			{
				get { return ExtensionInfo["StartFrom"].To<DateTime>(); }
				set { ExtensionInfo["StartFrom"] = value.Ticks; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2284Key)]
			[DescriptionLoc(LocalizedStrings.Str2285Key)]
			[PropertyOrder(1)]
			public int DayOffset
			{
				get { return ExtensionInfo["DayOffset"].To<int>(); }
				set { ExtensionInfo["DayOffset"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2617Key)]
			[DescriptionLoc(LocalizedStrings.Str2813Key)]
			[PropertyOrder(3)]
			public bool IsSystemOnly
			{
				get { return (bool)ExtensionInfo["IsSystemOnly"]; }
				set { ExtensionInfo["IsSystemOnly"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2814Key)]
			[DescriptionLoc(LocalizedStrings.Str2815Key)]
			[PropertyOrder(4)]
			public bool LoadEveningSession
			{
				get { return (bool)ExtensionInfo["LoadEveningSession"]; }
				set { ExtensionInfo["LoadEveningSession"] = value; }
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

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.TemporaryFilesKey)]
			[DescriptionLoc(LocalizedStrings.TemporaryFilesKey, true)]
			[PropertyOrder(6)]
			public TempFiles UseTemporaryFiles
			{
				get { return ExtensionInfo["UseTemporaryFiles"].To<TempFiles>(); }
				set { ExtensionInfo["UseTemporaryFiles"] = value.To<string>(); }
			}

			[Category(_usdRur)]
			[DisplayNameLoc(LocalizedStrings.XamlStr655Key)]
			[DescriptionLoc(LocalizedStrings.Str2817Key)]
			[PropertyOrder(0)]
			public bool IsDownloadUsdRate
			{
				get { return (bool)ExtensionInfo["IsDownloadUsdRate"]; }
				set { ExtensionInfo["IsDownloadUsdRate"] = value; }
			}

			[Category(_usdRur)]
			[DisplayNameLoc(LocalizedStrings.Str2282Key)]
			[DescriptionLoc(LocalizedStrings.Str2818Key)]
			[PropertyOrder(1)]
			public DateTime UsdRurStartFrom
			{
				get { return (DateTime)ExtensionInfo["UsdRurStartFrom"]; }
				set { ExtensionInfo["UsdRurStartFrom"] = value; }
			}

			[Category(_rtsStndard)]
			[DisplayNameLoc(LocalizedStrings.XamlStr655Key)]
			[DescriptionLoc(LocalizedStrings.Str2819Key)]
			[PropertyOrder(5)]
			public bool SaveRtsStdTrades
			{
				get { return (bool)ExtensionInfo["SaveRtsStdTrades"]; }
				set { ExtensionInfo["SaveRtsStdTrades"] = value; }
			}

			[Category(_rtsStndard)]
			[DisplayNameLoc(LocalizedStrings.Str2820Key)]
			[DescriptionLoc(LocalizedStrings.Str2821Key)]
			[PropertyOrder(5)]
			public bool SaveRtsStdCombinedOnly
			{
				get { return (bool)ExtensionInfo["SaveRtsStdCombinedOnly"]; }
				set { ExtensionInfo["SaveRtsStdCombinedOnly"] = value; }
			}
		}

		private RtsSettings _settings;

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new RtsSettings(settings);

			if (settings.IsDefault)
			{
				_settings.DayOffset = 3;
				_settings.StartFrom = RtsHistorySource.RtsMinAvaliableTime;
				_settings.UsdRurStartFrom = FortsDailyData.UsdRateMinAvailableTime;
				_settings.IsDownloadUsdRate = true;
				//_settings.UsdRateLastDate = FortsDailyData.UsdRateMinAvailableTime;
				_settings.IsSystemOnly = true;
				_settings.LoadEveningSession = false;
				_settings.SaveRtsStdTrades = false;
				_settings.SaveRtsStdCombinedOnly = false;
				_settings.Interval = TimeSpan.FromDays(1);
				_settings.IgnoreWeekends = true;
				_settings.UseTemporaryFiles = TempFiles.UseAndDelete;
			}
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override TimeSpan OnProcess()
		{
			var source = new RtsHistorySource
			{
				IsSystemOnly = _settings.IsSystemOnly,
				LoadEveningSession = _settings.LoadEveningSession,
				SaveRtsStdTrades = _settings.SaveRtsStdTrades,
				SaveRtsStdCombinedOnly = _settings.SaveRtsStdCombinedOnly
			};

			if (_settings.UseTemporaryFiles != TempFiles.NotUse)
				source.DumpFolder = GetTempPath();

			// если фильтр по инструментам выключен (выбран инструмент все инструменты)
			var allSecurity = this.GetAllSecurity();
			var usdRur = GetUsdRur(source.SecurityIdGenerator.GenerateId("USD/RUR", ExchangeBoard.Forts));

			var startDate = _settings.StartFrom;
			var endDate = DateTime.Today - TimeSpan.FromDays(_settings.DayOffset);

			var allDates = startDate.Range(endDate, TimeSpan.FromDays(1)).ToArray();

			var secMap = new HashSet<Security>();

			if (allSecurity == null)
				secMap.AddRange(Settings.Securities.Select(s => s.Security));

			foreach (var date in allDates)
			{
				if (!CanProcess())
					break;

				if (_settings.IgnoreWeekends && !ExchangeBoard.Forts.IsTradeDate(date.ApplyTimeZone(Exchange.Moex.TimeZoneInfo), true))
				{
					this.AddDebugLog(LocalizedStrings.WeekEndDate, date);
					continue;
				}

				this.AddInfoLog(LocalizedStrings.Str2823Params, date);

				var trades = source.LoadTrades(EntityRegistry.Securities, date);

				if (trades.Count == 0)
				{
					this.AddDebugLog(LocalizedStrings.NoData);
				}
				else
				{
					if (allSecurity == null)
						trades = trades.Where(p => secMap.Contains(p.Key)).ToDictionary();

					foreach (var pair in trades)
					{
						SaveSecurity(pair.Key);
						SaveTrades(pair.Key, pair.Value);
					}
				}

				if (_settings.UseTemporaryFiles == TempFiles.UseAndDelete)
				{
					var dir = source.GetDumpFile(null, date, date, typeof(Trade), null);

					if (Directory.Exists(dir))
						Directory.Delete(dir, true);
				}
				
				_settings.StartFrom = date.AddDays(1);
				SaveSettings();
			}

			if (_settings.IsDownloadUsdRate)
			{
				var usdRurStorage = StorageRegistry.GetTradeStorage(usdRur, _settings.Drive, _settings.StorageFormat);

				foreach (var date in _settings.UsdRurStartFrom
					.Range(endDate, TimeSpan.FromDays(1))
					.Except(usdRurStorage.Dates))
				{
					if (!CanProcess())
						break;

					if (_settings.IgnoreWeekends && !usdRur.Board.IsTradeDate(date.ApplyTimeZone(Exchange.Moex.TimeZoneInfo), true))
					{
						this.AddDebugLog(LocalizedStrings.WeekEndDate, date);
						continue;
					}

					this.AddInfoLog(LocalizedStrings.Str2294Params, date, usdRur.Id);

					var rate = FortsDailyData.GetRate(usdRur, date, date.Add(TimeSpan.FromDays(1)));

					if (rate.Count == 0)
					{
						this.AddDebugLog(LocalizedStrings.NoData);
					}
					else
					{
						SaveTrades(usdRur, rate.Select(p => new Trade
						{
							Security = usdRur,
							Price = p.Value,
							Time = p.Key,
						}).OrderBy(t => t.Time));	
					}

					_settings.UsdRurStartFrom = date.AddDays(1);
					SaveSettings();
				}	
			}

			if (CanProcess())
				this.AddInfoLog(LocalizedStrings.Str2300);

			return base.OnProcess();
		}

		private Security GetUsdRur(string secId)
		{
			var usdRur = EntityRegistry.Securities.ReadById(secId);

			if (usdRur != null)
				return usdRur;

			usdRur = new Security
			{
				Id = secId,
				Code = "USD/RUR",
				Name = LocalizedStrings.Str2824,
				Type = SecurityTypes.Index,
				ExtensionInfo = new Dictionary<object, object>(),
				Board = ExchangeBoard.Forts,
				PriceStep = 0.0001m,
			};

			SaveSecurity(usdRur);

			return usdRur;
		}
	}
}