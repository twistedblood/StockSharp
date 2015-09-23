namespace StockSharp.Hydra.Core
{
	using Ecng.Serialization;

	using StockSharp.Localization;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// ������ txt ��������.
	/// </summary>
	public class TemplateTxtRegistry : IPersistable
	{
		/// <summary>
		/// ������� <see cref="TemplateTxtRegistry"/>.
		/// </summary>
		public TemplateTxtRegistry()
		{
			TemplateTxtCandle = typeof(TimeFrameCandleMessage).GetTxtTemplate();
			TemplateTxtDepth = typeof(QuoteChangeMessage).GetTxtTemplate();
			TemplateTxtLevel1 = typeof(Level1ChangeMessage).GetTxtTemplate();
			TemplateTxtOrderLog = typeof(ExecutionMessage).GetTxtTemplate(ExecutionTypes.OrderLog);
			TemplateTxtSecurity = typeof(SecurityMessage).GetTxtTemplate();
			TemplateTxtTick = typeof(ExecutionMessage).GetTxtTemplate(ExecutionTypes.Tick);
			TemplateTxtTransaction = typeof(ExecutionMessage).GetTxtTemplate(ExecutionTypes.Order);
			TemplateTxtNews = typeof(NewsMessage).GetTxtTemplate();
		}

		/// <summary>
		/// ������ �������� � txt ��� ��������.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TemplateDepthKey)]
		[DescriptionLoc(LocalizedStrings.TemplateTxtDepthKey)]
		[PropertyOrder(0)]
		public string TemplateTxtDepth { get; set; }

		/// <summary>
		/// ������ �������� � txt ��� �����.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TemplateTickKey)]
		[DescriptionLoc(LocalizedStrings.TemplateTxtTickKey)]
		[PropertyOrder(1)]
		public string TemplateTxtTick { get; set; }

		/// <summary>
		/// ������ �������� � txt ��� ������.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TemplateCandleKey)]
		[DescriptionLoc(LocalizedStrings.TemplateTxtCandleKey)]
		[PropertyOrder(2)]
		public string TemplateTxtCandle { get; set; }

		/// <summary>
		/// ������ �������� � txt ��� level1.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TemplateLevel1Key)]
		[DescriptionLoc(LocalizedStrings.TemplateTxtLevel1Key)]
		[PropertyOrder(3)]
		public string TemplateTxtLevel1 { get; set; }

		/// <summary>
		/// ������ �������� � txt ��� ���� ������.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TemplateOrderLogKey)]
		[DescriptionLoc(LocalizedStrings.TemplateTxtOrderLogKey)]
		[PropertyOrder(4)]
		public string TemplateTxtOrderLog { get; set; }

		/// <summary>
		/// ������ �������� � txt ��� ����������.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TemplateTransactionKey)]
		[DescriptionLoc(LocalizedStrings.TemplateTxtTransactionKey)]
		[PropertyOrder(5)]
		public string TemplateTxtTransaction { get; set; }

		/// <summary>
		/// ������ �������� � txt ��� ������������.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TemplateSecurityKey)]
		[DescriptionLoc(LocalizedStrings.TemplateTxtSecurityKey)]
		[PropertyOrder(6)]
		public string TemplateTxtSecurity { get; set; }

		/// <summary>
		/// ������ �������� � txt ��� ��������.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TemplateNewsKey)]
		[DescriptionLoc(LocalizedStrings.TemplateTxtNewsKey)]
		[PropertyOrder(7)]
		public string TemplateTxtNews { get; set; }

		/// <summary>
		/// ��������� ���������.
		/// </summary>
		/// <param name="storage">��������� ��������.</param>
		public void Load(SettingsStorage storage)
		{
			TemplateTxtDepth = storage.GetValue("TemplateTxtDepth", TemplateTxtDepth);
			TemplateTxtTick = storage.GetValue("TemplateTxtTick", TemplateTxtTick);
			TemplateTxtCandle = storage.GetValue("TemplateTxtCandle", TemplateTxtCandle);
			TemplateTxtLevel1 = storage.GetValue("TemplateTxtLevel1", TemplateTxtLevel1);
			TemplateTxtOrderLog = storage.GetValue("TemplateTxtOrderLog", TemplateTxtOrderLog);
			TemplateTxtTransaction = storage.GetValue("TemplateTxtTransaction", TemplateTxtTransaction);
			TemplateTxtSecurity = storage.GetValue("TemplateTxtSecurity", TemplateTxtSecurity);
			TemplateTxtNews = storage.GetValue("TemplateTxtNews", TemplateTxtNews);
		}

		/// <summary>
		/// ��������� ���������.
		/// </summary>
		/// <param name="storage">��������� ��������.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("TemplateTxtDepth", TemplateTxtDepth);
			storage.SetValue("TemplateTxtTick", TemplateTxtTick);
			storage.SetValue("TemplateTxtCandle", TemplateTxtCandle);
			storage.SetValue("TemplateTxtLevel1", TemplateTxtLevel1);
			storage.SetValue("TemplateTxtOrderLog", TemplateTxtOrderLog);
			storage.SetValue("TemplateTxtTransaction", TemplateTxtTransaction);
			storage.SetValue("TemplateTxtSecurity", TemplateTxtSecurity);
			storage.SetValue("TemplateTxtNews", TemplateTxtNews);
		}
	}
}