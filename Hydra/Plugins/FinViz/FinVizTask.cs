namespace StockSharp.Hydra.FinViz
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using MoreLinq;

	using StockSharp.Algo.History;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2288ParamsKey, _sourceName)]
	[TaskDoc("http://stocksharp.com/doc/html/34b9a25a-ecb2-4d82-b342-3fa46ec155ba.htm")]
	[TaskIcon("finviz_logo.png")]
	[TaskCategory(TaskCategories.America | TaskCategories.History |
		TaskCategories.Stock | TaskCategories.Paid | TaskCategories.Level1)]
	class FinVizTask : BaseHydraTask, ISecurityDownloader
	{
		private const string _sourceName = "FinViz";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class FinVizSettings : HydraTaskSettings
		{
			public FinVizSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}
		}

		private FinVizSettings _settings;

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new FinVizSettings(settings);

			if (settings.IsDefault)
				_settings.Interval = TimeSpan.FromDays(1);
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		public override IEnumerable<Type> SupportedMarketDataTypes
		{
			get
			{
				return new[]
				{
					typeof(Level1ChangeMessage)
				}; 
			}
		}

		protected override TimeSpan OnProcess()
		{
			// если фильтр по инструментам выключен (выбран инструмент все инструменты)
			if (this.GetAllSecurity() != null)
			{
				//throw new InvalidOperationException("Источник не поддерживает закачку данных по всем инструментам.");
				this.AddWarningLog(LocalizedStrings.Str2292);
				return TimeSpan.MaxValue;
			}

			var hasSecurities = false;

			foreach (var secGroup in GetWorkingSecurities().Batch(20))
			{
				hasSecurities = true;

				if (!CanProcess())
					break;

				var source = new FinVizHistorySource();
				var changes = source.LoadChanges(secGroup.Select(s => s.Security));

				foreach (var pair in changes)
				{
					if (!CanProcess())
						break;

					SaveLevel1Changes(pair.Key, new[] { pair.Value });
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

		void ISecurityDownloader.Refresh(ISecurityStorage storage, Security criteria, Action<Security> newSecurity, Func<bool> isCancelled)
		{
			new FinVizHistorySource().Refresh(storage, criteria, newSecurity, isCancelled);
		}
	}
}