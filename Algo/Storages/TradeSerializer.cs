namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	class TradeMetaInfo : BinaryMetaInfo<TradeMetaInfo>
	{
		public TradeMetaInfo(DateTime date)
			: base(date)
		{
			FirstId = -1;
		}

		public override object LastId
		{
			get { return PrevId; }
		}

		public long FirstId { get; set; }
		public long PrevId { get; set; }

		public override void Write(Stream stream)
		{
			base.Write(stream);

			stream.Write(FirstId);
			stream.Write(PrevId);
			stream.Write(FirstPrice);
			stream.Write(LastPrice);

			WriteNonSystemPrice(stream);
			WriteFractionalVolume(stream);

			WriteLocalTime(stream, MarketDataVersions.Version47);

			if (Version < MarketDataVersions.Version50)
				return;

			stream.Write(ServerOffset);

			if (Version < MarketDataVersions.Version54)
				return;

			WriteOffsets(stream);
		}

		public override void Read(Stream stream)
		{
			base.Read(stream);

			FirstId = stream.Read<long>();
			PrevId = stream.Read<long>();
			FirstPrice = stream.Read<decimal>();
			LastPrice = stream.Read<decimal>();

			ReadNonSystemPrice(stream);
			ReadFractionalVolume(stream);

			ReadLocalTime(stream, MarketDataVersions.Version47);

			if (Version < MarketDataVersions.Version50)
				return;

			ServerOffset = stream.Read<TimeSpan>();

			if (Version < MarketDataVersions.Version54)
				return;

			ReadOffsets(stream);
		}

		public override void CopyFrom(TradeMetaInfo src)
		{
			base.CopyFrom(src);

			FirstId = src.FirstId;
			PrevId = src.PrevId;
			FirstPrice = src.FirstPrice;
			LastPrice = src.LastPrice;
		}
	}

	class TradeSerializer : BinaryMarketDataSerializer<ExecutionMessage, TradeMetaInfo>
	{
		public TradeSerializer(SecurityId securityId)
			: base(securityId, 50)
		{
			Version = MarketDataVersions.Version54;
		}

		protected override void OnSave(BitArrayWriter writer, IEnumerable<ExecutionMessage> messages, TradeMetaInfo metaInfo)
		{
			if (metaInfo.IsEmpty())
			{
				var first = messages.First();

				metaInfo.FirstId = metaInfo.PrevId = first.GetTradeId();
				metaInfo.ServerOffset = first.ServerTime.Offset;
			}

			writer.WriteInt(messages.Count());

			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version48;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version50;
			var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version54;

			foreach (var msg in messages)
			{
				if (msg.ExecutionType != ExecutionTypes.Tick)
					throw new ArgumentOutOfRangeException("messages", msg.ExecutionType, LocalizedStrings.Str1019Params.Put(msg.TradeId));

				var tradeId = msg.GetTradeId();

				// сделки для индексов имеют нулевой номер
				if (tradeId < 0)
					throw new ArgumentOutOfRangeException("messages", tradeId, LocalizedStrings.Str1020);

				// execution ticks (like option execution) may be a zero cost
				// ticks for spreads may be a zero cost or less than zero
				//if (msg.TradePrice < 0)
				//	throw new ArgumentOutOfRangeException("messages", msg.TradePrice, LocalizedStrings.Str1021Params.Put(msg.TradeId));

				metaInfo.PrevId = writer.SerializeId(tradeId, metaInfo.PrevId);

				// pyhta4og.
				// http://stocksharp.com/forum/yaf_postsm6450_Oshibka-pri-importie-instrumientov-s-Finama.aspx#post6450

				var volume = msg.Volume;

				if (metaInfo.Version < MarketDataVersions.Version53)
				{
					if (volume == null)
						throw new ArgumentException(LocalizedStrings.Str1022Params.Put((object)msg.TradeId ?? msg.TradeStringId), "messages");

					if (volume < 0)
						throw new ArgumentOutOfRangeException("messages", volume, LocalizedStrings.Str1022Params.Put(msg.TradeId));

					writer.WriteVolume(volume.Value, metaInfo, SecurityId);
				}
				else
				{
					writer.Write(volume != null);

					if (volume != null)
					{
						if (volume < 0)
							throw new ArgumentOutOfRangeException("messages", volume, LocalizedStrings.Str1022Params.Put(msg.TradeId));

						writer.WriteVolume(volume.Value, metaInfo, SecurityId);
					}
				}
				
				writer.WritePriceEx(msg.GetTradePrice(), metaInfo, SecurityId);
				writer.WriteSide(msg.OriginSide);

				var lastOffset = metaInfo.LastServerOffset;
				metaInfo.LastTime = writer.WriteTime(msg.ServerTime, metaInfo.LastTime, LocalizedStrings.Str985, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, ref lastOffset);
				metaInfo.LastServerOffset = lastOffset;

				if (metaInfo.Version < MarketDataVersions.Version40)
					continue;

				if (metaInfo.Version < MarketDataVersions.Version47)
					writer.WriteLong(SecurityId.GetLatency(msg.ServerTime, msg.LocalTime).Ticks);
				else
				{
					var hasLocalTime = true;

					if (metaInfo.Version >= MarketDataVersions.Version49)
					{
						hasLocalTime = !msg.LocalTime.IsDefault() && msg.LocalTime != msg.ServerTime;
						writer.Write(hasLocalTime);
					}

					if (hasLocalTime)
					{
						lastOffset = metaInfo.LastLocalOffset;
						metaInfo.LastLocalTime = writer.WriteTime(msg.LocalTime, metaInfo.LastLocalTime, LocalizedStrings.Str1024, allowNonOrdered, isUtc, metaInfo.LocalOffset, allowDiffOffsets, ref lastOffset);
						metaInfo.LastLocalOffset = lastOffset;
					}
				}

				if (metaInfo.Version < MarketDataVersions.Version42)
					continue;

				if (metaInfo.Version >= MarketDataVersions.Version51)
				{
					writer.Write(msg.IsSystem != null);

					if (msg.IsSystem != null)
						writer.Write(msg.IsSystem.Value);
				}
				else
					writer.Write(msg.IsSystem ?? true);

				if (msg.IsSystem == false)
				{
					if (metaInfo.Version >= MarketDataVersions.Version51)
						writer.WriteNullableInt(msg.TradeStatus);
					else
						writer.WriteInt(msg.TradeStatus ?? 0);
				}

				var oi = msg.OpenInterest;

				if (metaInfo.Version < MarketDataVersions.Version46)
					writer.WriteVolume(oi ?? 0m, metaInfo, SecurityId);
				else
				{
					writer.Write(oi != null);

					if (oi != null)
						writer.WriteVolume(oi.Value, metaInfo, SecurityId);
				}

				if (metaInfo.Version < MarketDataVersions.Version45)
					continue;

				writer.Write(msg.IsUpTick != null);

				if (msg.IsUpTick != null)
					writer.Write(msg.IsUpTick.Value);

				if (metaInfo.Version < MarketDataVersions.Version52)
					continue;

				writer.Write(msg.Currency != null);

				if (msg.Currency != null)
					writer.WriteInt((int)msg.Currency.Value);
			}
		}

		public override ExecutionMessage MoveNext(MarketDataEnumerator enumerator)
		{
			var reader = enumerator.Reader;
			var metaInfo = enumerator.MetaInfo;

			metaInfo.FirstId += reader.ReadLong();

			var volume = metaInfo.Version < MarketDataVersions.Version53
				? reader.ReadVolume(metaInfo)
				: reader.Read() ? reader.ReadVolume(metaInfo) : (decimal?)null;

			var price = reader.ReadPriceEx(metaInfo);

			var orderDirection = reader.Read() ? (reader.Read() ? Sides.Buy : Sides.Sell) : (Sides?)null;

			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version48;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version50;
			var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version54;

			var prevTime = metaInfo.FirstTime;
			var lastOffset = metaInfo.FirstServerOffset;
			var serverTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.GetTimeZone(isUtc, SecurityId), allowDiffOffsets, ref lastOffset);
			metaInfo.FirstTime = prevTime;
			metaInfo.FirstServerOffset = lastOffset;

			var msg = new ExecutionMessage
			{
				//LocalTime = metaInfo.FirstTime,
				ExecutionType = ExecutionTypes.Tick,
				SecurityId = SecurityId,
				TradeId = metaInfo.FirstId,
				Volume = volume,
				OriginSide = orderDirection,
				TradePrice = price,
				ServerTime = serverTime,
			};

			if (metaInfo.Version < MarketDataVersions.Version40)
				return msg;

			if (metaInfo.Version < MarketDataVersions.Version47)
			{
				msg.LocalTime = msg.ServerTime.LocalDateTime - reader.ReadLong().To<TimeSpan>() + metaInfo.LocalOffset;
			}
			else
			{
				var hasLocalTime = true;

				if (metaInfo.Version >= MarketDataVersions.Version49)
					hasLocalTime = reader.Read();

				if (hasLocalTime)
				{
					var prevLocalTime = metaInfo.FirstLocalTime;
					lastOffset = metaInfo.FirstLocalOffset;
					var localTime = reader.ReadTime(ref prevLocalTime, allowNonOrdered, isUtc, metaInfo.LocalOffset, allowDiffOffsets, ref lastOffset);
					metaInfo.FirstLocalTime = prevLocalTime;
					metaInfo.FirstLocalOffset = lastOffset;
					msg.LocalTime = localTime.LocalDateTime;
				}
				//else
				//	msg.LocalTime = msg.ServerTime;
			}

			if (metaInfo.Version < MarketDataVersions.Version42)
				return msg;

			msg.IsSystem = metaInfo.Version < MarketDataVersions.Version51
						? reader.Read()
						: (reader.Read() ? reader.Read() : (bool?)null);

			if (msg.IsSystem == false)
			{
				msg.TradeStatus = metaInfo.Version < MarketDataVersions.Version51
					? reader.ReadInt()
					: reader.ReadNullableInt<int>();
			}

			if (metaInfo.Version < MarketDataVersions.Version46 || reader.Read())
				msg.OpenInterest = reader.ReadVolume(metaInfo);

			if (metaInfo.Version < MarketDataVersions.Version45)
				return msg;

			if (reader.Read())
				msg.IsUpTick = reader.Read();

			if (metaInfo.Version >= MarketDataVersions.Version52)
			{
				if (reader.Read())
					msg.Currency = (CurrencyTypes)reader.ReadInt();
			}

			return msg;
		}
	}
}