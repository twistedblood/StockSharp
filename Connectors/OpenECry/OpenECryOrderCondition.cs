namespace StockSharp.OpenECry
{
	using System.ComponentModel;

	using Ecng.Collections;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The type of the conditional order OEC.
	/// </summary>
	public enum OpenECryStopType
	{
		/// <summary>
		/// The market order is automatically registered after reaching the stop price.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str242Key)]
		StopMarket,

		/// <summary>
		/// The limit order is automatically registered after reaching the stop price.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1733Key)]
		StopLimit,

		/// <summary>
		/// Stop price automatically follows the market, but only in a profitable direction for position, staying on specified in advance interval from market price. If the market reaches the stop price, the market order is automatically registered.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.TrailingStopLossKey)]
		TrailingStopMarket,

		/// <summary>
		/// As <see cref="OpenECryStopType.TrailingStopMarket"/>, but when it reaches the stop price the limit order is registered.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.TrailingStopLimitKey)]
		TrailingStopLimit
	}

	// OEC trailing stop description: 
	// http://www.openecry.com/cfbb/index.cfm?page=topic&topicID=532
	// http://www.openecry.com/cfbb/index.cfm?page=topic&topicID=225


	/// <summary>
	/// <see cref="OpenECry"/> order condition.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str2264Key, "OpenECry")]
	public class OpenECryOrderCondition : OrderCondition
	{
		internal enum AssetTypeEnum
		{
			All,
			Equity,
			Future
		}

		private const string _keyStopType = "StopType";
		private const string _keyStopPrice = "StopPrice";
		private const string _keyDelta = "Delta";
		private const string _keyIsPercentDelta = "IsPercentDelta";
		private const string _keyTriggerType = "TriggerType";
		private const string _keyReferencePrice = "ReferencePrice";
		private const string _keyAssetType = "AssetType";

		/// <summary>
		/// Initializes a new instance of the <see cref="OpenECryOrderCondition"/>.
		/// </summary>
		public OpenECryOrderCondition()
		{
		}

		/// <summary>
		/// Stop type.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2570Key)]
		[DescriptionLoc(LocalizedStrings.Str2571Key)]
		public OpenECryStopType? StopType
		{
			get { return (OpenECryStopType?)Parameters.TryGetValue(_keyStopType); }
			set { Parameters[_keyStopType] = value; }
		}

		/// <summary>
		/// Stop-price.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.StopPriceKey)]
		[DescriptionLoc(LocalizedStrings.StopPriceKey, true)]
		public decimal? StopPrice
		{
			get { return (decimal?)Parameters.TryGetValue(_keyStopPrice); }
			set { Parameters[_keyStopPrice] = value; }
		}

		/// <summary>
		/// Trailing stop follows the market if price change is larger than Delta.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayName("Trailing Delta")]
		[DescriptionLoc(LocalizedStrings.Str2572Key)]
		public decimal? Delta
		{
			get { return (decimal?)Parameters.TryGetValue(_keyDelta); }
			set { Parameters[_keyDelta] = value; }
		}

		/// <summary>
		/// <see langword="true" />, if <see cref="OpenECryOrderCondition.Delta"/> expressed in percentage.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2573Key)]
		[DescriptionLoc(LocalizedStrings.Str2574Key)]
		public bool? IsPercentDelta
		{
			get { return (bool?)Parameters.TryGetValue(_keyIsPercentDelta); }
			set { Parameters[_keyIsPercentDelta] = value; }
		}

		/// <summary>
		/// Trigger field.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str2575Key)]
		[DescriptionLoc(LocalizedStrings.Str2576Key)]
		public Level1Fields? TriggerType
		{
			get { return (Level1Fields?)Parameters.TryGetValue(_keyTriggerType); }
			set { Parameters[_keyTriggerType] = value; }
		}

		/// <summary>
		/// Trailing stop begins tracking once the price reaches ReferencePrice.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayName("Trailing ReferencePrice")]
		[DescriptionLoc(LocalizedStrings.Str2577Key)]
		public decimal? ReferencePrice
		{
			get { return (decimal?)Parameters.TryGetValue(_keyReferencePrice); }
			set { Parameters[_keyReferencePrice] = value; }
		}

		internal AssetTypeEnum? AssetType
		{
			get { return (AssetTypeEnum?)Parameters.TryGetValue(_keyAssetType); }
			set { Parameters[_keyAssetType] = value; }
		}
	}
}