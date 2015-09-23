namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	class OrderLogMetaInfo : BinaryMetaInfo<OrderLogMetaInfo>
	{
		public OrderLogMetaInfo(DateTime date)
			: base(date)
		{
			FirstOrderId = -1;
			Portfolios = new List<string>();
		}

		public override object LastId
		{
			get { return LastTransactionId; }
		}

		public long FirstOrderId { get; set; }
		public long LastOrderId { get; set; }

		public long FirstTradeId { get; set; }
		public long LastTradeId { get; set; }

		public long FirstTransactionId { get; set; }
		public long LastTransactionId { get; set; }

		public decimal FirstOrderPrice { get; set; }
		public decimal LastOrderPrice { get; set; }

		public IList<string> Portfolios { get; private set; }

		public override void Write(Stream stream)
		{
			base.Write(stream);

			stream.Write(FirstOrderId);
			stream.Write(FirstTradeId);
			stream.Write(LastOrderId);
			stream.Write(LastTradeId);
			stream.Write(FirstPrice);
			stream.Write(LastPrice);

			if (Version < MarketDataVersions.Version34)
				return;

			stream.Write(FirstTransactionId);
			stream.Write(LastTransactionId);

			if (Version < MarketDataVersions.Version40)
				return;

			stream.Write(Portfolios.Count);

			foreach (var portfolio in Portfolios)
				stream.Write(portfolio);

			WriteNonSystemPrice(stream);
			WriteFractionalVolume(stream);

			if (Version < MarketDataVersions.Version45)
				return;

			stream.Write(FirstOrderPrice);
			stream.Write(LastOrderPrice);

			WriteLocalTime(stream, MarketDataVersions.Version46);

			if (Version < MarketDataVersions.Version48)
				return;

			stream.Write(ServerOffset);

			if (Version < MarketDataVersions.Version52)
				return;

			WriteOffsets(stream);
		}

		public override void Read(Stream stream)
		{
			base.Read(stream);

			FirstOrderId = stream.Read<long>();
			FirstTradeId = stream.Read<long>();
			LastOrderId = stream.Read<long>();
			LastTradeId = stream.Read<long>();
			FirstPrice = stream.Read<decimal>();
			LastPrice = stream.Read<decimal>();

			if (Version < MarketDataVersions.Version34)
				return;

			FirstTransactionId = stream.Read<long>();
			LastTransactionId = stream.Read<long>();

			if (Version < MarketDataVersions.Version40)
				return;

			var count = stream.Read<int>();

			for (var i = 0; i < count; i++)
				Portfolios.Add(stream.Read<string>());

			ReadNonSystemPrice(stream);
			ReadFractionalVolume(stream);

			if (Version < MarketDataVersions.Version45)
				return;

			FirstOrderPrice = stream.Read<decimal>();
			LastOrderPrice = stream.Read<decimal>();

			ReadLocalTime(stream, MarketDataVersions.Version46);

			if (Version < MarketDataVersions.Version48)
				return;

			ServerOffset = stream.Read<TimeSpan>();

			if (Version < MarketDataVersions.Version52)
				return;

			ReadOffsets(stream);
		}

		public override void CopyFrom(OrderLogMetaInfo src)
		{
			base.CopyFrom(src);

			FirstOrderId = src.FirstOrderId;
			FirstTradeId = src.FirstTradeId;
			LastOrderId = src.LastOrderId;
			LastTradeId = src.LastTradeId;

			FirstPrice = src.FirstPrice;
			LastPrice = src.LastPrice;

			FirstTransactionId = src.FirstTransactionId;
			LastTransactionId = src.LastTransactionId;

			FirstOrderPrice = src.FirstOrderPrice;
			LastOrderPrice = src.LastOrderPrice;

			Portfolios.Clear();
			Portfolios.AddRange(src.Portfolios);
		}
	}

	class OrderLogSerializer : BinaryMarketDataSerializer<ExecutionMessage, OrderLogMetaInfo>
	{
		public OrderLogSerializer(SecurityId securityId)
			: base(securityId, 200)
		{
			Version = MarketDataVersions.Version52;
		}

		protected override void OnSave(BitArrayWriter writer, IEnumerable<ExecutionMessage> items, OrderLogMetaInfo metaInfo)
		{
			if (metaInfo.IsEmpty() && !items.IsEmpty())
			{
				var item = items.First();

				metaInfo.FirstOrderId = metaInfo.LastOrderId = item.SafeGetOrderId();
				metaInfo.FirstTransactionId = metaInfo.LastTransactionId = item.TransactionId;
				metaInfo.ServerOffset = item.ServerTime.Offset;
			}

			writer.WriteInt(items.Count());

			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version47;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version48;
			var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version52;

			foreach (var item in items)
			{
				var hasTrade = item.TradeId != null || item.TradePrice != null;

				var orderId = item.SafeGetOrderId();
				if (orderId < 0)
					throw new ArgumentOutOfRangeException("items", orderId, LocalizedStrings.Str925);

				// sell market orders has zero price (if security do not have min allowed price)
				// execution ticks (like option execution) may be a zero cost
				// ticks for spreads may be a zero cost or less than zero
				//if (item.Price < 0)
				//	throw new ArgumentOutOfRangeException("items", item.Price, LocalizedStrings.Str926Params.Put(item.OrderId));

				var volume = item.SafeGetVolume();
				if (volume <= 0)
					throw new ArgumentOutOfRangeException("items", volume, LocalizedStrings.Str927Params.Put(item.OrderId));

				long? tradeId = null;

				if (hasTrade)
				{
					tradeId = item.GetTradeId();

					if (tradeId <= 0)
						throw new ArgumentOutOfRangeException("items", tradeId, LocalizedStrings.Str1012Params.Put(item.OrderId));

					// execution ticks (like option execution) may be a zero cost
					// ticks for spreads may be a zero cost or less than zero
					//if (item.TradePrice <= 0)
					//	throw new ArgumentOutOfRangeException("items", item.TradePrice, LocalizedStrings.Str929Params.Put(item.TradeId, item.OrderId));
				}

				metaInfo.LastOrderId = writer.SerializeId(orderId, metaInfo.LastOrderId);

				var orderPrice = item.Price;

				if (metaInfo.Version < MarketDataVersions.Version45)
					writer.WritePriceEx(orderPrice, metaInfo, SecurityId);
				else
				{
					var isAligned = (orderPrice % metaInfo.PriceStep) == 0;
					writer.Write(isAligned);

					if (isAligned)
					{
						if (metaInfo.FirstOrderPrice == 0)
							metaInfo.FirstOrderPrice = metaInfo.LastOrderPrice = orderPrice;

						writer.WritePrice(orderPrice, metaInfo.LastOrderPrice, metaInfo, SecurityId, true);
						metaInfo.LastOrderPrice = orderPrice;
					}
					else
					{
						if (metaInfo.FirstNonSystemPrice == 0)
							metaInfo.FirstNonSystemPrice = metaInfo.LastNonSystemPrice = orderPrice;

						metaInfo.LastNonSystemPrice = writer.WriteDecimal(orderPrice, metaInfo.LastNonSystemPrice);
					}
				}

				writer.WriteVolume(volume, metaInfo, SecurityId);

				writer.Write(item.Side == Sides.Buy);

				var lastOffset = metaInfo.LastServerOffset;
				metaInfo.LastTime = writer.WriteTime(item.ServerTime, metaInfo.LastTime, LocalizedStrings.Str1013, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, ref lastOffset);
				metaInfo.LastServerOffset = lastOffset;

				if (hasTrade)
				{
					writer.Write(true);

					if (metaInfo.FirstTradeId == 0)
					{
						metaInfo.FirstTradeId = metaInfo.LastTradeId = tradeId.Value;
					}

					metaInfo.LastTradeId = writer.SerializeId(tradeId.Value, metaInfo.LastTradeId);

					writer.WritePriceEx(item.GetTradePrice(), metaInfo, SecurityId);
				}
				else
				{
					writer.Write(false);
					writer.Write(item.OrderState == OrderStates.Active);
				}

				if (metaInfo.Version < MarketDataVersions.Version31)
					continue;

				writer.WriteNullableInt(item.OrderStatus);

				if (metaInfo.Version < MarketDataVersions.Version33)
					continue;

				if (metaInfo.Version < MarketDataVersions.Version50)
					writer.WriteInt((int)(item.TimeInForce ?? TimeInForce.PutInQueue));
				else
				{
					writer.Write(item.TimeInForce != null);

					if (item.TimeInForce != null)
						writer.WriteInt((int)item.TimeInForce.Value);
				}

				if (metaInfo.Version >= MarketDataVersions.Version49)
				{
					writer.Write(item.IsSystem != null);

					if (item.IsSystem != null)
						writer.Write(item.IsSystem.Value);
				}
				else
					writer.Write(item.IsSystem ?? true);

				if (metaInfo.Version < MarketDataVersions.Version34)
					continue;

				metaInfo.LastTransactionId = writer.SerializeId(item.TransactionId, metaInfo.LastTransactionId);

				if (metaInfo.Version < MarketDataVersions.Version40)
					continue;

				if (metaInfo.Version < MarketDataVersions.Version46)
					writer.WriteLong(0/*item.Latency.Ticks*/);

				var portfolio = item.PortfolioName;
				var isEmptyPf = portfolio == null || portfolio == Portfolio.AnonymousPortfolio.Name;

				writer.Write(!isEmptyPf);

				if (!isEmptyPf)
				{
					metaInfo.Portfolios.TryAdd(item.PortfolioName);
					writer.WriteInt(metaInfo.Portfolios.IndexOf(item.PortfolioName));	
				}

				if (metaInfo.Version < MarketDataVersions.Version51)
					continue;

				writer.Write(item.Currency != null);

				if (item.Currency != null)
					writer.WriteInt((int)item.Currency.Value);
			}
		}

		public override ExecutionMessage MoveNext(MarketDataEnumerator enumerator)
		{
			var reader = enumerator.Reader;
			var metaInfo = enumerator.MetaInfo;

			metaInfo.FirstOrderId += reader.ReadLong();

			decimal price;

			if (metaInfo.Version < MarketDataVersions.Version45)
			{
				price = reader.ReadPriceEx(metaInfo);
			}
			else
			{
				if (reader.Read())
					price = metaInfo.FirstOrderPrice = reader.ReadPrice(metaInfo.FirstOrderPrice, metaInfo, true);
				else
					price = metaInfo.FirstNonSystemPrice = reader.ReadDecimal(metaInfo.FirstNonSystemPrice);
			}

			var volume = reader.ReadVolume(metaInfo);

			var orderDirection = reader.Read() ? Sides.Buy : Sides.Sell;

			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version47;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version48;
			var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version52;

			var prevTime = metaInfo.FirstTime;
			var lastOffset = metaInfo.FirstServerOffset;
			var serverTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.GetTimeZone(isUtc, SecurityId), allowDiffOffsets, ref lastOffset);
			metaInfo.FirstTime = prevTime;
			metaInfo.FirstServerOffset = lastOffset;

			var execMsg = new ExecutionMessage
			{
				//LocalTime = metaInfo.FirstTime,
				ExecutionType = ExecutionTypes.OrderLog,
				SecurityId = SecurityId,
				OrderId = metaInfo.FirstOrderId,
				Volume = volume,
				Side = orderDirection,
				ServerTime = serverTime,
				Price = price,
			};

			if (reader.Read())
			{
				metaInfo.FirstTradeId += reader.ReadLong();
				price = reader.ReadPriceEx(metaInfo);

				execMsg.TradeId = metaInfo.FirstTradeId;
				execMsg.TradePrice = price;

				execMsg.OrderState = OrderStates.Done;
			}
			else
			{
				var active = reader.Read();
				execMsg.OrderState = active ? OrderStates.Active : OrderStates.Done;
				execMsg.IsCancelled = !active;
			}

			if (metaInfo.Version >= MarketDataVersions.Version31)
			{
				execMsg.OrderStatus = reader.ReadNullableInt<OrderStatus>();

				if (execMsg.OrderStatus != null)
				{
					var status = (int)execMsg.OrderStatus.Value;

					if (status.HasBits(0x01))
						execMsg.TimeInForce = TimeInForce.PutInQueue;
					else if (status.HasBits(0x02))
						execMsg.TimeInForce = TimeInForce.CancelBalance;
				}

				// Лучше ExecCond писать отдельным полем так как возможно только Плаза пишет это в статус
				if (metaInfo.Version >= MarketDataVersions.Version33)
				{
					if (metaInfo.Version < MarketDataVersions.Version50)
						execMsg.TimeInForce = (TimeInForce)reader.ReadInt();
					else
						execMsg.TimeInForce = reader.Read() ? (TimeInForce)reader.ReadInt() : (TimeInForce?)null;

					execMsg.IsSystem = metaInfo.Version < MarketDataVersions.Version49
						? reader.Read()
						: (reader.Read() ? reader.Read() : (bool?)null);

					if (metaInfo.Version >= MarketDataVersions.Version34)
					{
						metaInfo.FirstTransactionId += reader.ReadLong();
						execMsg.TransactionId = metaInfo.FirstTransactionId;
					}
				}
				else
				{
					if (execMsg.OrderStatus != null)
						execMsg.IsSystem = !((int)execMsg.OrderStatus).HasBits(0x04);
				}
			}

			if (metaInfo.Version >= MarketDataVersions.Version40)
			{
				if (metaInfo.Version < MarketDataVersions.Version46)
					/*item.Latency =*/reader.ReadLong();//.To<TimeSpan>();

				if (reader.Read())
				{
					execMsg.PortfolioName = metaInfo.Portfolios[reader.ReadInt()];
				}
			}

			//if (order.Portfolio == null)
			//	order.Portfolio = Portfolio.AnonymousPortfolio;

			if (metaInfo.Version >= MarketDataVersions.Version51)
			{
				if (reader.Read())
					execMsg.Currency = (CurrencyTypes)reader.ReadInt();
			}

			return execMsg;
		}
	}
}