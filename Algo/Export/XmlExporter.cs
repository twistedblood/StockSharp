namespace StockSharp.Algo.Export
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Xml;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// ������� � xml.
	/// </summary>
	public class XmlExporter : BaseExporter
	{
		private const string _timeFormat = "yyyy-MM-dd HH:mm:ss.fff zzz";

		/// <summary>
		/// ������� <see cref="XmlExporter"/>.
		/// </summary>
		/// <param name="security">����������.</param>
		/// <param name="arg">�������� ������.</param>
		/// <param name="isCancelled">����������, ������������ ������� ���������� ��������.</param>
		/// <param name="fileName">���� � �����.</param>
		public XmlExporter(Security security, object arg, Func<int, bool> isCancelled, string fileName)
			: base(security, arg, isCancelled, fileName)
		{
		}

		/// <summary>
		/// �������������� <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="messages">���������.</param>
		protected override void Export(IEnumerable<ExecutionMessage> messages)
		{
			switch ((ExecutionTypes)Arg)
			{
				case ExecutionTypes.Tick:
				{
					Do(messages, "trades", (writer, trade) =>
					{
						writer.WriteStartElement("trade");

						writer.WriteAttribute("id", trade.TradeId == null ? trade.TradeStringId : trade.TradeId.To<string>());
						writer.WriteAttribute("serverTime", trade.ServerTime.ToString(_timeFormat));
						writer.WriteAttribute("localTime", trade.LocalTime.ToString(_timeFormat));
						writer.WriteAttribute("price", trade.TradePrice);
						writer.WriteAttribute("volume", trade.Volume);

						if (trade.OriginSide != null)
							writer.WriteAttribute("originSide", trade.OriginSide.Value);

						if (trade.OpenInterest != null)
							writer.WriteAttribute("openInterest", trade.OpenInterest.Value);

						if (trade.IsUpTick != null)
							writer.WriteAttribute("isUpTick", trade.IsUpTick.Value);

						writer.WriteEndElement();
					});

					break;
				}
				case ExecutionTypes.OrderLog:
				{
					Do(messages, "orderLog", (writer, item) =>
					{
						writer.WriteStartElement("item");

						writer.WriteAttribute("id", item.OrderId == null ? item.OrderStringId : item.OrderId.To<string>());
						writer.WriteAttribute("serverTime", item.ServerTime.ToString(_timeFormat));
						writer.WriteAttribute("localTime", item.LocalTime.ToString(_timeFormat));
						writer.WriteAttribute("price", item.Price);
						writer.WriteAttribute("volume", item.Volume);
						writer.WriteAttribute("side", item.Side);
						writer.WriteAttribute("state", item.OrderState);
						writer.WriteAttribute("timeInForce", item.TimeInForce);
						writer.WriteAttribute("isSystem", item.IsSystem);

						if (item.TradePrice != null)
						{
							writer.WriteAttribute("tradeId", item.TradeId == null ? item.TradeStringId : item.TradeId.To<string>());
							writer.WriteAttribute("tradePrice", item.TradePrice);

							if (item.OpenInterest != null)
								writer.WriteAttribute("openInterest", item.OpenInterest.Value);
						}

						writer.WriteEndElement();
					});

					break;
				}
				case ExecutionTypes.Order:
				case ExecutionTypes.Trade:
				{
					Do(messages, "executions", (writer, item) =>
					{
						writer.WriteStartElement("item");

						writer.WriteAttribute("serverTime", item.ServerTime.ToString(_timeFormat));
						writer.WriteAttribute("localTime", item.LocalTime.ToString(_timeFormat));
						writer.WriteAttribute("portfolio", item.PortfolioName);
						writer.WriteAttribute("transactionId", item.TransactionId);
						writer.WriteAttribute("id", item.OrderId == null ? item.OrderStringId : item.OrderId.To<string>());
						writer.WriteAttribute("price", item.Price);
						writer.WriteAttribute("volume", item.Volume);
						writer.WriteAttribute("balance", item.Balance);
						writer.WriteAttribute("side", item.Side);
						writer.WriteAttribute("type", item.OrderType);
						writer.WriteAttribute("state", item.OrderState);
						writer.WriteAttribute("tradeId", item.TradeId == null ? item.TradeStringId : item.TradeId.To<string>());
						writer.WriteAttribute("tradePrice", item.TradePrice);

						writer.WriteEndElement();
					});

					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// �������������� <see cref="QuoteChangeMessage"/>.
		/// </summary>
		/// <param name="messages">���������.</param>
		protected override void Export(IEnumerable<QuoteChangeMessage> messages)
		{
			Do(messages, "depths", (writer, depth) =>
			{
				writer.WriteStartElement("depth");

				writer.WriteAttribute("serverTime", depth.ServerTime.ToString(_timeFormat));
				writer.WriteAttribute("localTime", depth.LocalTime.ToString(_timeFormat));

				foreach (var quote in depth.Bids.Concat(depth.Asks).OrderByDescending(q => q.Price))
				{
					writer.WriteStartElement("quote");

					writer.WriteAttribute("price", quote.Price);
					writer.WriteAttribute("volume", quote.Volume);
					writer.WriteAttribute("side", quote.Side);

					writer.WriteEndElement();
				}

				writer.WriteEndElement();
			});
		}

		/// <summary>
		/// �������������� <see cref="Level1ChangeMessage"/>.
		/// </summary>
		/// <param name="messages">���������.</param>
		protected override void Export(IEnumerable<Level1ChangeMessage> messages)
		{
			Do(messages, "messages", (writer, message) =>
			{
				writer.WriteStartElement("message");

				writer.WriteAttribute("serverTime", message.ServerTime.ToString(_timeFormat));
				writer.WriteAttribute("localTime", message.LocalTime.ToString(_timeFormat));

				foreach (var pair in message.Changes)
					writer.WriteAttribute(pair.Key.ToString(), pair.Value is DateTime ? ((DateTime)pair.Value).ToString(_timeFormat) : pair.Value);

				writer.WriteEndElement();
			});
		}

		/// <summary>
		/// �������������� <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="messages">���������.</param>
		protected override void Export(IEnumerable<CandleMessage> messages)
		{
			Do(messages, "candles", (writer, candle) =>
			{
				writer.WriteStartElement("candle");

				writer.WriteAttribute("openTime", candle.OpenTime.ToString(_timeFormat));
				writer.WriteAttribute("closeTime", candle.CloseTime.ToString(_timeFormat));

				writer.WriteAttribute("O", candle.OpenPrice);
				writer.WriteAttribute("H", candle.HighPrice);
				writer.WriteAttribute("L", candle.LowPrice);
				writer.WriteAttribute("C", candle.ClosePrice);
				writer.WriteAttribute("V", candle.TotalVolume);

				if (candle.OpenInterest != null)
					writer.WriteAttribute("openInterest", candle.OpenInterest.Value);

				writer.WriteEndElement();
			});
		}

		/// <summary>
		/// �������������� <see cref="NewsMessage"/>.
		/// </summary>
		/// <param name="messages">���������.</param>
		protected override void Export(IEnumerable<NewsMessage> messages)
		{
			Do(messages, "news", (writer, n) =>
			{
				writer.WriteStartElement("item");

				if (!n.Id.IsEmpty())
					writer.WriteAttribute("id", n.Id);

				writer.WriteAttribute("serverTime", n.ServerTime.ToString(_timeFormat));
				writer.WriteAttribute("localTime", n.LocalTime.ToString(_timeFormat));

				if (n.SecurityId != null)
					writer.WriteAttribute("securityCode", n.SecurityId.Value.SecurityCode);

				if (!n.BoardCode.IsEmpty())
					writer.WriteAttribute("boardCode", n.BoardCode);

				writer.WriteAttribute("headline", n.Headline);

				if (!n.Source.IsEmpty())
					writer.WriteAttribute("source", n.Source);

				if (n.Url != null)
					writer.WriteAttribute("board", n.Url);

				if (!n.Story.IsEmpty())
					writer.WriteCData(n.Story);

				writer.WriteEndElement();
			});
		}

		/// <summary>
		/// �������������� <see cref="SecurityMessage"/>.
		/// </summary>
		/// <param name="messages">���������.</param>
		protected override void Export(IEnumerable<SecurityMessage> messages)
		{
			Do(messages, "securities", (writer, security) =>
			{
				writer.WriteStartElement("security");

				writer.WriteAttribute("code", security.SecurityId.SecurityCode);
				writer.WriteAttribute("board", security.SecurityId.BoardCode);

				if (!security.Name.IsEmpty())
					writer.WriteAttribute("name", security.Name);

				if (!security.ShortName.IsEmpty())
					writer.WriteAttribute("shortName", security.ShortName);

				if (security.PriceStep != null)
					writer.WriteAttribute("priceStep", security.PriceStep.Value);

				if (security.VolumeStep != null)
					writer.WriteAttribute("volumeStep", security.VolumeStep.Value);

				if (security.Multiplier != null)
					writer.WriteAttribute("multiplier", security.Multiplier.Value);

				if (security.Decimals != null)
					writer.WriteAttribute("decimals", security.Decimals.Value);

				if (security.Currency != null)
					writer.WriteAttribute("currency", security.Currency.Value);

				if (security.SecurityType != null)
					writer.WriteAttribute("type", security.SecurityType.Value);

				if (security.OptionType != null)
					writer.WriteAttribute("optionType", security.OptionType.Value);

				if (security.Strike != null)
					writer.WriteAttribute("strike", security.Strike.Value);

				if (!security.BinaryOptionType.IsEmpty())
					writer.WriteAttribute("binaryOptionType", security.BinaryOptionType);

				if (!security.UnderlyingSecurityCode.IsEmpty())
					writer.WriteAttribute("underlyingSecurityCode", security.UnderlyingSecurityCode);

				if (security.ExpiryDate != null)
					writer.WriteAttribute("expiryDate", security.ExpiryDate.Value.ToString("yyyy-MM-dd"));

				if (security.SettlementDate != null)
					writer.WriteAttribute("settlementDate", security.SettlementDate.Value.ToString("yyyy-MM-dd"));

				if (!security.SecurityId.Bloomberg.IsEmpty())
					writer.WriteAttribute("bloomberg", security.SecurityId.Bloomberg);

				if (!security.SecurityId.Cusip.IsEmpty())
					writer.WriteAttribute("cusip", security.SecurityId.Cusip);

				if (!security.SecurityId.IQFeed.IsEmpty())
					writer.WriteAttribute("iqfeed", security.SecurityId.IQFeed);

				if (security.SecurityId.InteractiveBrokers != null)
					writer.WriteAttribute("ib", security.SecurityId.InteractiveBrokers);

				if (!security.SecurityId.Isin.IsEmpty())
					writer.WriteAttribute("isin", security.SecurityId.Isin);

				if (!security.SecurityId.Plaza.IsEmpty())
					writer.WriteAttribute("plaza", security.SecurityId.Plaza);

				if (!security.SecurityId.Ric.IsEmpty())
					writer.WriteAttribute("ric", security.SecurityId.Ric);

				if (!security.SecurityId.Sedol.IsEmpty())
					writer.WriteAttribute("sedol", security.SecurityId.Sedol);

				writer.WriteEndElement();
			});
		}

		private void Do<TValue>(IEnumerable<TValue> values, string rootElem, Action<XmlWriter, TValue> action)
		{
			using (var writer = XmlWriter.Create(Path, new XmlWriterSettings { Indent = true }))
			{
				writer.WriteStartElement(rootElem);

				foreach (var value in values)
				{
					if (!CanProcess())
						break;

					action(writer, value);
				}

				writer.WriteEndElement();
			}
		}
	}
}