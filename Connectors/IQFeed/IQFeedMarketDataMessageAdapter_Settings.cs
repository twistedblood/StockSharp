namespace StockSharp.IQFeed
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// The messages adapter for IQFeed.
	/// </summary>
	[DisplayName("IQFeed")]
	[CategoryLoc(LocalizedStrings.AmericaKey)]
	[DescriptionLoc(LocalizedStrings.Str1770Key, "IQFeed")]
	[CategoryOrderLoc(LocalizedStrings.Str174Key, 0)]
	[CategoryOrderLoc(LocalizedStrings.Str2121Key, 1)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 2)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 3)]
	partial class IQFeedMarketDataMessageAdapter
	{
		private EndPoint _level1Address = IQFeedAddresses.DefaultLevel1Address;

		/// <summary>
		/// Address for obtaining data on Level1.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str2122Key)]
		[DescriptionLoc(LocalizedStrings.Str2123Key)]
		[PropertyOrder(1)]
		public EndPoint Level1Address
		{
			get { return _level1Address; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_level1Address = value;
			}
		}

		private EndPoint _level2Address = IQFeedAddresses.DefaultLevel2Address;

		/// <summary>
		/// Address for obtaining data on Level2.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str2124Key)]
		[DescriptionLoc(LocalizedStrings.Str2125Key)]
		[PropertyOrder(2)]
		public EndPoint Level2Address
		{
			get { return _level2Address; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_level2Address = value;
			}
		}

		private EndPoint _lookupAddress = IQFeedAddresses.DefaultLookupAddress;

		/// <summary>
		/// Address for obtaining history data.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str2126Key)]
		[DescriptionLoc(LocalizedStrings.Str2127Key)]
		[PropertyOrder(3)]
		public EndPoint LookupAddress
		{
			get { return _lookupAddress; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_lookupAddress = value;
			}
		}

		private EndPoint _adminAddress = IQFeedAddresses.DefaultAdminAddress;

		/// <summary>
		/// Address for obtaining service data.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str2128Key)]
		[DescriptionLoc(LocalizedStrings.Str2129Key)]
		[PropertyOrder(4)]
		public EndPoint AdminAddress
		{
			get { return _adminAddress; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_adminAddress = value;
			}
		}

		private EndPoint _derivativeAddress = IQFeedAddresses.DefaultDerivativeAddress;

		/// <summary>
		/// Address for obtaining derivative data.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str2128Key)]
		[DescriptionLoc(LocalizedStrings.Str2130Key)]
		[PropertyOrder(5)]
		public EndPoint DerivativeAddress
		{
			get { return _derivativeAddress; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_derivativeAddress = value;
			}
		}

		private IQFeedLevel1Column[] _level1Columns;

		/// <summary>
		/// All <see cref="IQFeedLevel1Column"/> to be transmit.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str2121Key)]
		[DisplayNameLoc(LocalizedStrings.Str2131Key)]
		[DescriptionLoc(LocalizedStrings.Str2132Key)]
		[PropertyOrder(0)]
		public IQFeedLevel1Column[] Level1Columns
		{
			get { return _level1Columns; }
			set
			{
				_level1Columns = value
					.Where(c =>
						c != Level1ColumnRegistry.Symbol &&
						c != Level1ColumnRegistry.ExchangeId &&
						c != Level1ColumnRegistry.LastTradeMarket &&
						c != Level1ColumnRegistry.BidMarket &&
						c != Level1ColumnRegistry.AskMarket)
					.ToArray();
			}
		}

		private IEnumerable<SecurityTypes> _securityTypesFilter = Enumerator.GetValues<SecurityTypes>();

		/// <summary>
		/// Securities types, for which data must be received.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str2121Key)]
		[DisplayNameLoc(LocalizedStrings.Str2133Key)]
		[DescriptionLoc(LocalizedStrings.Str2134Key)]
		[PropertyOrder(1)]
		public IEnumerable<SecurityTypes> SecurityTypesFilter
		{
			get { return _securityTypesFilter; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_securityTypesFilter = value;
			}
		}

		/// <summary>
		/// Whether to load instruments from the archive of the IQFeed site. The default is off.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str2121Key)]
		[DisplayNameLoc(LocalizedStrings.Str2135Key)]
		[DescriptionLoc(LocalizedStrings.Str2136Key)]
		[PropertyOrder(2)]
		public bool IsDownloadSecurityFromSite { get; set; }

		/// <summary>
		/// Path to file with IQFeed list of securities, downloaded from the website. If path is specified, then secondary download from website does not occur, and only the local copy gets parsed.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str2121Key)]
		[DisplayNameLoc(LocalizedStrings.Str2137Key)]
		[DescriptionLoc(LocalizedStrings.Str2138Key)]
		[PropertyOrder(3)]
		public string SecuritiesFile { get; set; }

		/// <summary>
		/// The parameters validity check.
		/// </summary>
		[Browsable(false)]
		public override bool IsValid
		{
			get { return true; }
		}

		/// <summary>
		/// The list of all available <see cref="IQFeedLevel1Column"/>.
		/// </summary>
		[Browsable(false)]
		public IQFeedLevel1ColumnRegistry Level1ColumnRegistry { get; private set; }

		private static readonly HashSet<TimeSpan> _timeFrames = new HashSet<TimeSpan>(new[]
		{
			TimeSpan.FromMinutes(1),
			TimeSpan.FromMinutes(5),
			TimeSpan.FromMinutes(10),
			TimeSpan.FromMinutes(15),
			TimeSpan.FromMinutes(30),
			TimeSpan.FromHours(1),
			TimeSpan.FromDays(1),
			TimeSpan.FromDays(7),
			TimeSpan.FromTicks(TimeHelper.TicksPerMonth)
		});

		/// <summary>
		/// Available time frames.
		/// </summary>
		[Browsable(false)]
		public static IEnumerable<TimeSpan> TimeFrames
		{
			get { return _timeFrames; }
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Level1Address = storage.GetValue<EndPoint>("Level1Address");
			Level2Address = storage.GetValue<EndPoint>("Level2Address");
			LookupAddress = storage.GetValue<EndPoint>("LookupAddress");
			AdminAddress = storage.GetValue<EndPoint>("AdminAddress");
			DerivativeAddress = storage.GetValue<EndPoint>("DerivativeAddress");

			IsDownloadSecurityFromSite = storage.GetValue<bool>("IsDownloadSecurityFromSite");
			SecuritiesFile = storage.GetValue<string>("SecuritiesFile");

			SecurityTypesFilter = storage
									.GetValue<string>("SecurityTypesFilter")
									.Split(",")
									.Select(name => name.To<SecurityTypes>())
									.ToArray();

			Level1Columns = storage
								.GetValue<string>("Level1Columns")
								.Split(",")
								.Select(name => Level1ColumnRegistry[name])
								.ToArray();
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Level1Address", Level1Address.To<string>());
			storage.SetValue("Level2Address", Level2Address.To<string>());
			storage.SetValue("LookupAddress", LookupAddress.To<string>());
			storage.SetValue("AdminAddress", AdminAddress.To<string>());
			storage.SetValue("DerivativeAddress", DerivativeAddress.To<string>());

			storage.SetValue("IsDownloadSecurityFromSite", IsDownloadSecurityFromSite);
			storage.SetValue("SecuritiesFile", SecuritiesFile);

			storage.SetValue("SecurityTypesFilter", SecurityTypesFilter.Select(t => t.To<string>()).Join(","));
			storage.SetValue("Level1Columns", Level1Columns.Select(c => c.Name).Join(","));
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return "Level1 = {0} Level2 = {1}".Put(Level1Address, Level2Address);
		}
	}
}