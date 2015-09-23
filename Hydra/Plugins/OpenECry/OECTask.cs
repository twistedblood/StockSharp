namespace StockSharp.Hydra.OpenECry
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Algo.Candles;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.OpenECry;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[TaskDoc("http://stocksharp.com/doc/html/4d84a1e0-fe23-4b14-8323-c5f68f117cc7.htm")]
	[TaskIcon("oec_logo.png")]
	[TaskCategory(TaskCategories.America | TaskCategories.RealTime | TaskCategories.Stock |
		TaskCategories.Free | TaskCategories.Ticks | TaskCategories.MarketDepth | TaskCategories.Forex |
		TaskCategories.Level1 | TaskCategories.Candles | TaskCategories.Transactions)]
	class OECTask : ConnectorHydraTask<OECTrader>
	{
		private const string _sourceName = "OpenECry";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class OECSettings : ConnectorHydraTaskSettings
		{
			public OECSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.AddressKey, true)]
			[PropertyOrder(0)]
			public EndPoint Address
			{
				get { return ExtensionInfo["Address"].To<EndPoint>(); }
				set { ExtensionInfo["Address"] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.LoginKey, true)]
			[PropertyOrder(1)]
			public string Login
			{
				get { return (string)ExtensionInfo["Login"]; }
				set { ExtensionInfo["Login"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
			[PropertyOrder(2)]
			public SecureString Password
			{
				get { return ExtensionInfo["Password"].To<SecureString>(); }
				set { ExtensionInfo["Password"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayName("UUID")]
			[DescriptionLoc(LocalizedStrings.Str2565Key)]
			[PropertyOrder(3)]
			public string Uuid
			{
				get { return (string)ExtensionInfo.TryGetValue("Uuid"); }
				set { ExtensionInfo["Uuid"] = value; }
			}

			[Browsable(true)]
			public override bool IsDownloadNews
			{
				get { return base.IsDownloadNews; }
				set { base.IsDownloadNews = value; }
			}
		}

		public OECTask()
		{
			_supportedCandleSeries = OpenECryMessageAdapter.TimeFrames.Select(tf => new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = tf
			}).ToArray();
		}

		private OECSettings _settings;

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		private readonly IEnumerable<CandleSeries> _supportedCandleSeries;

		public override IEnumerable<CandleSeries> SupportedCandleSeries
		{
			get { return _supportedCandleSeries; }
		}

		protected override MarketDataConnector<OECTrader> CreateConnector(HydraTaskSettings settings)
		{
			_settings = new OECSettings(settings);

			if (settings.IsDefault)
			{
				_settings.Address = OpenECryAddresses.Api;
				_settings.Uuid = string.Empty;
				_settings.Login = string.Empty;
				_settings.Password = new SecureString();
				_settings.IsDownloadNews = true;
				_settings.SupportedLevel1Fields = Enumerator.GetValues<Level1Fields>();
			}

			return new MarketDataConnector<OECTrader>(EntityRegistry.Securities, this, () => new OECTrader
			{
				Uuid = _settings.Uuid,
				Login = _settings.Login,
				Password = _settings.Password.To<string>(),
				Address = _settings.Address
			});
		}
	}
}