namespace StockSharp.Algo.Testing
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Преобразователь сообщений вида <see cref="QuoteChangeMessage"/> и <see cref="ExecutionMessage"/> (ассоциированный с тиковой сделкой)
	/// в единый поток <see cref="ExecutionMessage"/> (ассоциированный с логом заявок).
	/// </summary>
	class ExecutionLogConverter
	{
		private readonly Random _volumeRandom = new Random(TimeHelper.Now.Millisecond);
		private readonly Random _priceRandom = new Random(TimeHelper.Now.Millisecond);
		private readonly SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>> _bids;
		private readonly SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>> _asks;
		private decimal _currSpreadPrice;
		private readonly MarketEmulatorSettings _settings;
		private readonly Func<DateTime, DateTimeOffset> _getServerTime;
		private decimal _prevTickPrice;
		// указывает, есть ли реальные стаканы, чтобы своей псевдо генерацией не портить настоящую историю
		private DateTime _lastDepthDate;
		//private DateTime _lastTradeDate;
		private SecurityMessage _securityDefinition = new SecurityMessage
		{
			PriceStep = 1,
			VolumeStep = 1,
		};
		private bool _priceStepUpdated;
		private bool _volumeStepUpdated;

		private decimal? _prevBidPrice;
		private decimal? _prevBidVolume;
		private decimal? _prevAskPrice;
		private decimal? _prevAskVolume;

		public ExecutionLogConverter(SecurityId securityId,
			SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>> bids,
			SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>> asks,
			MarketEmulatorSettings settings, Func<DateTime, DateTimeOffset> getServerTime)
		{
			if (bids == null)
				throw new ArgumentNullException("bids");

			if (asks == null)
				throw new ArgumentNullException("asks");

			if (settings == null)
				throw new ArgumentNullException("settings");

			if (getServerTime == null)
				throw new ArgumentNullException("getServerTime");

			_bids = bids;
			_asks = asks;
			_settings = settings;
			_getServerTime = getServerTime;
			SecurityId = securityId;
		}

		/// <summary>
		/// Идентификатор инструмента.
		/// </summary>
		public SecurityId SecurityId { get; private set; }

		/// <summary>
		/// Преобразовать котировки.
		/// </summary>
		/// <param name="message">Котировки.</param>
		/// <returns>Поток <see cref="ExecutionMessage"/>.</returns>
		public IEnumerable<ExecutionMessage> ToExecutionLog(QuoteChangeMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (!_priceStepUpdated || !_volumeStepUpdated)
			{
				var quote = message.GetBestBid() ?? message.GetBestAsk();

				if (quote != null)
				{
					_securityDefinition.PriceStep = quote.Price.GetDecimalInfo().EffectiveScale.GetPriceStep();
					_securityDefinition.VolumeStep = quote.Volume.GetDecimalInfo().EffectiveScale.GetPriceStep();
					
					_priceStepUpdated = true;
					_volumeStepUpdated = true;
				}
			}

			_lastDepthDate = message.LocalTime.Date;

			// чтобы склонировать внутренние котировки
			//message = (QuoteChangeMessage)message.Clone();
			// TODO для ускорения идет shallow copy котировок
			var newBids = message.IsSorted ? message.Bids : message.Bids.OrderByDescending(q => q.Price);
			var newAsks = message.IsSorted ? message.Asks : message.Asks.OrderBy(q => q.Price);

			return ProcessQuoteChange(message.LocalTime, message.ServerTime, newBids.ToArray(), newAsks.ToArray());
		}

		private IEnumerable<ExecutionMessage> ProcessQuoteChange(DateTime time, DateTimeOffset serverTime, QuoteChange[] newBids, QuoteChange[] newAsks)
		{
			decimal bestBidPrice;
			decimal bestAskPrice;

			var diff = new List<ExecutionMessage>();

			GetDiff(diff, time, serverTime, _bids, newBids, Sides.Buy, out bestBidPrice);
			GetDiff(diff, time, serverTime, _asks, newAsks, Sides.Sell, out bestAskPrice);

			var spreadPrice = bestAskPrice == 0
				? bestBidPrice
				: (bestBidPrice == 0
					? bestAskPrice
					: (bestAskPrice - bestBidPrice) / 2 + bestBidPrice);

			try
			{
				//при обновлении стакана необходимо учитывать направление сдвига, чтобы не было ложного исполнения при наложении бидов и асков.
				//т.е. если цена сдвинулась вниз, то обновление стакана необходимо начинать с минимального бида.
				return (spreadPrice < _currSpreadPrice)
					? diff.OrderBy(m => m.Price)
					: diff.OrderByDescending(m => m.Price);
			}
			finally
			{
				_currSpreadPrice = spreadPrice;
			}
		}

		private void GetDiff(List<ExecutionMessage> diff, DateTime time, DateTimeOffset serverTime, SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>> from, IEnumerable<QuoteChange> to, Sides side, out decimal newBestPrice)
		{
			newBestPrice = 0;

			var canProcessFrom = true;
			var canProcessTo = true;

			QuoteChange currFrom = null;
			QuoteChange currTo = null;

			// TODO
			//List<ExecutionMessage> currOrders = null;

			var mult = side == Sides.Buy ? -1 : 1;
			bool? isSpread = null;

			using (var fromEnum = from.GetEnumerator())
			using (var toEnum = to.GetEnumerator())
			{
				while (true)
				{
					if (canProcessFrom && currFrom == null)
					{
						if (!fromEnum.MoveNext())
							canProcessFrom = false;
						else
						{
							currFrom = fromEnum.Current.Value.Second;
							isSpread = isSpread == null;
						}
					}

					if (canProcessTo && currTo == null)
					{
						if (!toEnum.MoveNext())
							canProcessTo = false;
						else
						{
							currTo = toEnum.Current;

							if (newBestPrice == 0)
								newBestPrice = currTo.Price;
						}
					}

					if (currFrom == null)
					{
						if (currTo == null)
							break;
						else
						{
							AddExecMsg(diff, time, serverTime, currTo, currTo.Volume, false);
							currTo = null;
						}
					}
					else
					{
						if (currTo == null)
						{
							AddExecMsg(diff, time, serverTime, currFrom, -currFrom.Volume, isSpread.Value);
							currFrom = null;
						}
						else
						{
							if (currFrom.Price == currTo.Price)
							{
								if (currFrom.Volume != currTo.Volume)
								{
									AddExecMsg(diff, time, serverTime, currTo, currTo.Volume - currFrom.Volume, isSpread.Value);
								}

								currFrom = currTo = null;
							}
							else if (currFrom.Price * mult > currTo.Price * mult)
							{
								AddExecMsg(diff, time, serverTime, currTo, currTo.Volume, isSpread.Value);
								currTo = null;
							}
							else
							{
								AddExecMsg(diff, time, serverTime, currFrom, -currFrom.Volume, isSpread.Value);
								currFrom = null;
							}
						}
					}
				}
			}
		}

		private readonly RandomArray<bool> _isMatch = new RandomArray<bool>(100);

		private void AddExecMsg(List<ExecutionMessage> diff, DateTime time, DateTimeOffset serverTime, QuoteChange quote, decimal volume, bool isSpread)
		{
			if (volume > 0)
				diff.Add(CreateMessage(time, serverTime, quote.Side, quote.Price, volume));
			else
			{
				volume = volume.Abs();

				// matching only top orders (spread)
				if (isSpread && volume > 1 && _isMatch.Next())
				{
					var tradeVolume = (int)volume / 2;

					diff.Add(new ExecutionMessage
					{
						Side = quote.Side,
						Volume = tradeVolume,
						ExecutionType = ExecutionTypes.Tick,
						SecurityId = SecurityId,
						LocalTime = time,
						ServerTime = serverTime,
						TradePrice = quote.Price,
					});

					// that tick will not affect on order book
					//volume -= tradeVolume;
				}

				diff.Add(CreateMessage(time, serverTime, quote.Side, quote.Price, volume, true));
			}
		}

		/// <summary>
		/// Преобразовать тиковую сделку.
		/// </summary>
		/// <param name="message">Тиковая сделка.</param>
		/// <returns>Поток <see cref="ExecutionMessage"/>.</returns>
		public IEnumerable<ExecutionMessage> ToExecutionLog(ExecutionMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (!_priceStepUpdated)
			{
				_securityDefinition.PriceStep = message.GetTradePrice().GetDecimalInfo().EffectiveScale.GetPriceStep();
				_priceStepUpdated = true;
			}

			if (!_volumeStepUpdated)
			{
				_securityDefinition.VolumeStep = message.SafeGetVolume().GetDecimalInfo().EffectiveScale.GetPriceStep();
				_volumeStepUpdated = true;
			}

			//if (message.DataType != ExecutionDataTypes.Trade)
			//	throw new ArgumentOutOfRangeException("Тип данных не может быть {0}.".Put(message.DataType), "message");

			//_lastTradeDate = message.LocalTime.Date;

			return ProcessExecution(message);
		}

		private IEnumerable<ExecutionMessage> ProcessExecution(ExecutionMessage message)
		{
			var retVal = new List<ExecutionMessage>();

			var bestBid = _bids.FirstOrDefault();
			var bestAsk = _asks.FirstOrDefault();

			var tradePrice = message.GetTradePrice();
			var volume = message.Volume ?? 1;
			var time = message.LocalTime;

			if (bestBid.Value != null && tradePrice <= bestBid.Key)
			{
				// тик попал в биды, значит была крупная заявка по рынку на продажу,
				// которая возможна исполнила наши заявки

				ProcessMarketOrder(retVal, _bids, message.ServerTime, message.LocalTime, Sides.Sell, tradePrice, volume);

				// подтягиваем противоположные котировки и снимаем лишние заявки
				TryCreateOppositeOrder(retVal, _asks, time, message.ServerTime, tradePrice, volume, Sides.Buy);
			}
			else if (bestAsk.Value != null && tradePrice >= bestAsk.Key)
			{
				// тик попал в аски, значит была крупная заявка по рынку на покупку,
				// которая возможна исполнила наши заявки

				ProcessMarketOrder(retVal, _asks, message.ServerTime, message.LocalTime, Sides.Buy, tradePrice, volume);

				TryCreateOppositeOrder(retVal, _bids, time, message.ServerTime, tradePrice, volume, Sides.Sell);
			}
			else if (bestBid.Value != null && bestAsk.Value != null && bestBid.Key < tradePrice && tradePrice < bestAsk.Key)
			{
				// тик попал в спред, значит в спреде до сделки была заявка.
				// создаем две лимитки с разных сторон, но одинаковой ценой.
				// если в эмуляторе есть наша заявка на этом уровне, то она исполниться.
				// если нет, то эмулятор взаимно исполнит эти заявки друг об друга

				var originSide = GetOrderSide(message);

				retVal.Add(CreateMessage(time, message.ServerTime, originSide, tradePrice, volume + (_securityDefinition.VolumeStep ?? 1 * _settings.VolumeMultiplier), tif: TimeInForce.MatchOrCancel));

				var spreadStep = _settings.SpreadSize * GetPriceStep();

				// try to fill depth gaps

				var newBestPrice = tradePrice + spreadStep;

				var depth = _settings.MaxDepth;
				while (--depth > 0)
				{
					var diff = bestAsk.Key - newBestPrice;

					if (diff > 0)
					{
						retVal.Add(CreateMessage(time, message.ServerTime, Sides.Sell, newBestPrice, 0));
						newBestPrice += spreadStep * _priceRandom.Next(1, _settings.SpreadSize);
					}
					else
						break;
				}

				newBestPrice = tradePrice - spreadStep;

				depth = _settings.MaxDepth;
				while (--depth > 0)
				{
					var diff = newBestPrice - bestBid.Key;

					if (diff > 0)
					{
						retVal.Add(CreateMessage(time, message.ServerTime, Sides.Buy, newBestPrice, 0));
						newBestPrice -= spreadStep * _priceRandom.Next(1, _settings.SpreadSize);
					}
					else
						break;
				}

				retVal.Add(CreateMessage(time, message.ServerTime, originSide.Invert(), tradePrice, volume, tif: TimeInForce.MatchOrCancel));
			}
			else
			{
				// если у нас стакан был полу пустой, то тик формирует некий ценовой уровень в стакана,
				// так как прошедщая заявка должна была обо что-то удариться. допускаем, что после
				// прохождения сделки на этом ценовом уровне остался объем равный тиковой сделки

				var hasOpposite = true;

				Sides originSide;

				// определяем направление псевдо-ранее существовавшей заявки, из которой получился тик
				if (bestBid.Value != null)
					originSide = Sides.Sell;
				else if (bestAsk.Value != null)
					originSide = Sides.Buy;
				else
				{
					originSide = GetOrderSide(message);
					hasOpposite = false;
				}

				retVal.Add(CreateMessage(time, message.ServerTime, originSide, tradePrice, volume));

				// если стакан был полностью пустой, то формируем сразу уровень с противоположной стороны
				if (!hasOpposite)
				{
					var oppositePrice = tradePrice + _settings.SpreadSize * GetPriceStep() * (originSide == Sides.Buy ? 1 : -1);

					if (oppositePrice > 0)
						retVal.Add(CreateMessage(time, message.ServerTime, originSide.Invert(), oppositePrice, volume));
				}
			}

			if (!HasDepth(time))
			{
				// если стакан слишком разросся, то удаляем его хвосты (не удаляя пользовательские заявки)
				CancelWorstQuote(retVal, time, message.ServerTime, Sides.Buy, _bids);
				CancelWorstQuote(retVal, time, message.ServerTime, Sides.Sell, _asks);	
			}

			_prevTickPrice = tradePrice;

			return retVal;
		}

		private Sides GetOrderSide(ExecutionMessage message)
		{
			if (message.OriginSide == null)
				return message.TradePrice > _prevTickPrice ? Sides.Sell : Sides.Buy;
			else
				return message.OriginSide.Value.Invert();
		}

		/// <summary>
		/// Преобразовать первый уровень маркет-данных.
		/// </summary>
		/// <param name="message">Первый уровень маркет-данных.</param>
		/// <returns>Поток <see cref="Message"/>.</returns>
		public IEnumerable<Message> ToExecutionLog(Level1ChangeMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (message.IsContainsTick())
				yield return message.ToTick();

			if (message.IsContainsQuotes())
			{
				var prevBidPrice = _prevBidPrice;
				var prevBidVolume = _prevBidVolume;
				var prevAskPrice = _prevAskPrice;
				var prevAskVolume = _prevAskVolume;

				_prevBidPrice = (decimal?)message.Changes.TryGetValue(Level1Fields.BestBidPrice) ?? _prevBidPrice;
				_prevBidVolume = (decimal?)message.Changes.TryGetValue(Level1Fields.BestBidVolume) ?? _prevBidVolume;
				_prevAskPrice = (decimal?)message.Changes.TryGetValue(Level1Fields.BestAskPrice) ?? _prevAskPrice;
				_prevAskVolume = (decimal?)message.Changes.TryGetValue(Level1Fields.BestAskVolume) ?? _prevAskVolume;

				if (_prevBidPrice == 0)
					_prevBidPrice = null;

				if (_prevAskPrice == 0)
					_prevAskPrice = null;

				if (prevBidPrice == _prevBidPrice && prevBidVolume == _prevBidVolume && prevAskPrice == _prevAskPrice && prevAskVolume == _prevAskVolume)
					yield break;

				yield return new QuoteChangeMessage
				{
					SecurityId = message.SecurityId,
					LocalTime = message.LocalTime,
					ServerTime = message.ServerTime,
					Bids = _prevBidPrice == null ? Enumerable.Empty<QuoteChange>() : new[] { new QuoteChange(Sides.Buy, _prevBidPrice.Value, _prevBidVolume ?? 0) },
					Asks = _prevAskPrice == null ? Enumerable.Empty<QuoteChange>() : new[] { new QuoteChange(Sides.Sell, _prevAskPrice.Value, _prevAskVolume ?? 0) },
				};
			}
		}

		private void ProcessMarketOrder(List<ExecutionMessage> retVal, SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>> quotes, DateTimeOffset time, DateTime localTime, Sides orderSide, decimal tradePrice, decimal volume)
		{
			// вычисляем объем заявки по рынку, который смог бы пробить текущие котировки.

			// bigOrder - это наша большая рыночная заявка, которая способствовала появлению tradeMessage
			var bigOrder = CreateMessage(localTime, time, orderSide, tradePrice, 0, tif: TimeInForce.MatchOrCancel);
			var sign = orderSide == Sides.Buy ? -1 : 1;
			var hasQuotes = false;

			foreach (var pair in quotes)
			{
				var quote = pair.Value.Second;

				if (quote.Price * sign > tradePrice * sign)
				{
					bigOrder.Volume += quote.Volume;
				}
				else
				{
					if (quote.Price == tradePrice)
					{
						bigOrder.Volume += volume;

						//var diff = tradeMessage.Volume - quote.Volume;

						//// если объем котиовки был меньше объема сделки
						//if (diff > 0)
						//	retVal.Add(CreateMessage(tradeMessage.LocalTime, quote.Side, quote.Price, diff));
					}
					else
					{
						if ((tradePrice - quote.Price).Abs() == _securityDefinition.PriceStep)
						{
							// если на один шаг цены выше/ниже есть котировка, то не выполняем никаких действий
							// иначе добавляем новый уровень в стакан, чтобы не было большого расхождения цен.
							hasQuotes = true;
						}
					
						break;
					}

					//// если котировки с ценой сделки вообще не было в стакане
					//else if (quote.Price * sign < tradeMessage.TradePrice * sign)
					//{
					//	retVal.Add(CreateMessage(tradeMessage.LocalTime, quote.Side, tradeMessage.Price, tradeMessage.Volume));
					//}
				}
			}

			retVal.Add(bigOrder);

			// если собрали все котировки, то оставляем заявку в стакане по цене сделки
			if (!hasQuotes)
				retVal.Add(CreateMessage(localTime, time, orderSide.Invert(), tradePrice, volume));
		}

		private void TryCreateOppositeOrder(List<ExecutionMessage> retVal, SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>> quotes, DateTime localTime, DateTimeOffset serverTime, decimal tradePrice, decimal volume, Sides originSide)
		{
			if (HasDepth(localTime))
				return;

			var oppositePrice = tradePrice + _settings.SpreadSize * GetPriceStep() * (originSide == Sides.Buy ? 1 : -1);

			var bestQuote = quotes.FirstOrDefault();

			if (bestQuote.Value == null || ((originSide == Sides.Buy && oppositePrice < bestQuote.Key) || (originSide == Sides.Sell && oppositePrice > bestQuote.Key)))
				retVal.Add(CreateMessage(localTime, serverTime, originSide.Invert(), oppositePrice, volume));
		}

		private void CancelWorstQuote(List<ExecutionMessage> retVal, DateTime time, DateTimeOffset serverTime, Sides side, SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>> quotes)
		{
			if (quotes.Count <= _settings.MaxDepth)
				return;

			var worst = quotes.Last();
			var volume = worst.Value.First.Where(e => e.PortfolioName == null).Sum(e => e.Volume.Value);

			if (volume == 0)
				return;

			retVal.Add(CreateMessage(time, serverTime, side, worst.Key, volume, true));
		}

		private ExecutionMessage CreateMessage(DateTime localTime, DateTimeOffset serverTime, Sides side, decimal price, decimal volume, bool isCancelling = false, TimeInForce tif = TimeInForce.PutInQueue)
		{
			if (price <= 0)
				throw new ArgumentOutOfRangeException("price", price, LocalizedStrings.Str1144);

			//if (volume <= 0)
			//	throw new ArgumentOutOfRangeException("volume", volume, "Объем задан не верно.");

			if (volume == 0)
				volume = _volumeRandom.Next(10, 100);

			return new ExecutionMessage
			{
				Side = side,
				Price = price,
				Volume = volume,
				ExecutionType = ExecutionTypes.OrderLog,
				IsCancelled = isCancelling,
				SecurityId = SecurityId,
				LocalTime = localTime,
				ServerTime = serverTime,
				TimeInForce = tif,
			};
		}

		private bool HasDepth(DateTime time)
		{
			return _lastDepthDate == time.Date;
		}

		/// <summary>
		/// Преобразовать транзакцию.
		/// </summary>
		/// <param name="message">Транзакция.</param>
		/// <param name="quotesVolume">Объем в стакане.</param>
		/// <returns>Поток <see cref="ExecutionMessage"/>.</returns>
		public IEnumerable<ExecutionMessage> ToExecutionLog(OrderMessage message, decimal quotesVolume)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			var serverTime = _getServerTime(message.LocalTime);

			switch (message.Type)
			{
				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;

					if (_settings.IncreaseDepthVolume && NeedCheckVolume(regMsg.Side, regMsg.Price) && quotesVolume < regMsg.Volume)
					{
						foreach (var executionMessage in IncreaseDepthVolume(regMsg.LocalTime, serverTime, regMsg.Side, regMsg.Volume - quotesVolume))
							yield return executionMessage;
					}

					yield return new ExecutionMessage
					{
						LocalTime = regMsg.LocalTime,
						ServerTime = serverTime,
						SecurityId = regMsg.SecurityId,
						ExecutionType = ExecutionTypes.Order,
						TransactionId = regMsg.TransactionId,
						Price = regMsg.Price,
						Volume = regMsg.Volume,
						Side = regMsg.Side,
						PortfolioName = regMsg.PortfolioName,
						OrderType = regMsg.OrderType,
						UserOrderId = regMsg.UserOrderId
					};

					yield break;
				}
				case MessageTypes.OrderReplace:
				{
					var replaceMsg = (OrderReplaceMessage)message;

					if (_settings.IncreaseDepthVolume && NeedCheckVolume(replaceMsg.Side, replaceMsg.Price) && quotesVolume < replaceMsg.Volume)
					{
						foreach (var executionMessage in IncreaseDepthVolume(replaceMsg.LocalTime, serverTime, replaceMsg.Side, replaceMsg.Volume - quotesVolume))
							yield return executionMessage;
					}

					yield return new ExecutionMessage
					{
						LocalTime = replaceMsg.LocalTime,
						ServerTime = serverTime,
						SecurityId = replaceMsg.SecurityId,
						ExecutionType = ExecutionTypes.Order,
						IsCancelled = true,
						OrderId = replaceMsg.OldOrderId,
						OriginalTransactionId = replaceMsg.OldTransactionId,
						TransactionId = replaceMsg.TransactionId,
						PortfolioName = replaceMsg.PortfolioName,
						OrderType = replaceMsg.OrderType,
						// для старой заявки пользовательский идентификатор менять не надо
						//UserOrderId = replaceMsg.UserOrderId
					};

					yield return new ExecutionMessage
					{
						LocalTime = replaceMsg.LocalTime,
						ServerTime = serverTime,
						SecurityId = replaceMsg.SecurityId,
						ExecutionType = ExecutionTypes.Order,
						TransactionId = replaceMsg.TransactionId,
						Price = replaceMsg.Price,
						Volume = replaceMsg.Volume,
						Side = replaceMsg.Side,
						PortfolioName = replaceMsg.PortfolioName,
						OrderType = replaceMsg.OrderType,
						UserOrderId = replaceMsg.UserOrderId
					};

					yield break;
				}
				case MessageTypes.OrderCancel:
				{
					var cancelMsg = (OrderCancelMessage)message;

					yield return new ExecutionMessage
					{
						ExecutionType = ExecutionTypes.Order,
						IsCancelled = true,
						OrderId = cancelMsg.OrderId,
						TransactionId = cancelMsg.TransactionId,
						OriginalTransactionId = cancelMsg.OrderTransactionId,
						PortfolioName = cancelMsg.PortfolioName,
						SecurityId = cancelMsg.SecurityId,
						LocalTime = cancelMsg.LocalTime,
						ServerTime = serverTime,
						OrderType = cancelMsg.OrderType,
						// при отмене заявки пользовательский идентификатор не меняется
						//UserOrderId = cancelMsg.UserOrderId
					};

					yield break;
				}

				case MessageTypes.OrderPairReplace:
				case MessageTypes.OrderGroupCancel:
					throw new NotSupportedException();

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private decimal? GetBestPrice(Sides orderSide)
		{
			var quotes = orderSide == Sides.Buy ? _asks : _bids;

			var quote = quotes.FirstOrDefault();

			if (quote.Value != null)
				return quote.Key;

			return null;
		}

		private bool NeedCheckVolume(Sides orderSide, decimal price)
		{
			var bestPrice = GetBestPrice(orderSide);

			if (bestPrice == null)
				return false;

			return orderSide == Sides.Buy ? price >= bestPrice.Value : price <= bestPrice.Value;
		}

		private IEnumerable<ExecutionMessage> IncreaseDepthVolume(DateTime time, DateTimeOffset serverTime, Sides orderSide, decimal leftVolume)
		{
			var quotes = orderSide == Sides.Buy ? _asks : _bids;
			var quote = quotes.LastOrDefault();

			if(quote.Value == null)
				yield break;

			var side = orderSide.Invert();

			var lastVolume = quote.Value.Second.Volume;
			var lastPrice = quote.Value.Second.Price;

			while (leftVolume > 0 && lastPrice != 0)
			{
				lastVolume *= 2;
				lastPrice += GetPriceStep() * (side == Sides.Buy ? -1 : 1);

				leftVolume -= lastVolume;

				yield return CreateMessage(time, serverTime, side, lastPrice, lastVolume);
			}
		}

		private decimal GetPriceStep()
		{
			return _securityDefinition.PriceStep ?? 0.01m;
		}

		public void UpdateSecurityDefinition(SecurityMessage securityDefinition)
		{
			if (securityDefinition == null)
				throw new ArgumentNullException("securityDefinition");

			_securityDefinition = securityDefinition;

			_priceStepUpdated = _securityDefinition.PriceStep != null;
			_volumeStepUpdated = _securityDefinition.VolumeStep != null;
		}
	}
}