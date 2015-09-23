namespace StockSharp.Btce
{
	using System;
	using System.Linq;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Btce.Native;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The messages adapter for BTC-e.
	/// </summary>
	partial class BtceMessageAdapter
	{
		private long _lastMyTradeId;
		private bool _hasActiveOrders;
		private bool _hasMyTrades;
		private bool _requestOrderFirst;
		private readonly Dictionary<long, RefPair<long, decimal>> _orderInfo = new Dictionary<long, RefPair<long, decimal>>();

		private string GetPortfolioName()
		{
			return Key.To<string>().GetHashCode().To<string>();
		}

		private void ProcessOrderRegister(OrderRegisterMessage regMsg)
		{
			var reply = _client.MakeOrder(
				regMsg.SecurityId.SecurityCode.Replace('/', '_').ToLowerInvariant(),
				regMsg.Side.ToBtce(),
				regMsg.Price,
				regMsg.Volume
			);

			_orderInfo.Add(reply.Command.OrderId, RefTuple.Create(regMsg.TransactionId, regMsg.Volume));

			SendOutMessage(new ExecutionMessage
			{
				OriginalTransactionId = regMsg.TransactionId,
				OrderId = reply.Command.OrderId,
				Balance = (decimal)reply.Command.Remains,
				OrderState = OrderStates.Active,
				ExecutionType = ExecutionTypes.Order
			});

			ProcessFunds(reply.Command.Funds);

			_hasActiveOrders = true;
		}

		private void ProcessOrderCancel(OrderCancelMessage cancelMsg)
		{
			if (cancelMsg.OrderId == null)
				throw new InvalidOperationException(LocalizedStrings.Str2252Params.Put(cancelMsg.OrderTransactionId));

			var reply = _client.CancelOrder(cancelMsg.OrderId.Value);

			SendOutMessage(new ExecutionMessage
			{
				OriginalTransactionId = cancelMsg.TransactionId,
				OrderId = cancelMsg.OrderId,
				OrderState = OrderStates.Done,
				ExecutionType = ExecutionTypes.Order
			});

			ProcessFunds(reply.Command.Funds);
		}

		private void ProcessOrder(Order order)
		{
			var info = _orderInfo[order.Id];

			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Order,
				OrderId = order.Id,
				OriginalTransactionId = info.First,
				Price = (decimal)order.Price,
				Balance = info.Second,
				Volume = (decimal)order.Volume,
				Side = order.Side.ToStockSharp(),
				SecurityId = new SecurityId
				{
					SecurityCode = order.Instrument.ToStockSharpCode(),
					BoardCode = _boardCode,
				},
				ServerTime = order.Timestamp.ApplyTimeZone(TimeHelper.Moscow),
				PortfolioName = GetPortfolioName(),
				OrderState = order.Status.ToOrderState()
			});
		}

		private void ProcessExecution(Trade trade)
		{
			if (!trade.IsMyOrder)
				return;

			if (_lastMyTradeId >= trade.Id)
				return;

			_hasMyTrades = true;

			_lastMyTradeId = trade.Id;

			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Trade,
				OrderId = trade.OrderId,
				TradeId = trade.Id,
				TradePrice = (decimal)trade.Price,
				Volume = (decimal)trade.Volume,
				Side = trade.Side.ToStockSharp(),
				SecurityId = new SecurityId
				{
					SecurityCode = trade.Instrument.ToStockSharpCode(),
					BoardCode = _boardCode,
				},
				ServerTime = trade.Timestamp.ApplyTimeZone(TimeHelper.Moscow),
				PortfolioName = GetPortfolioName(),
			});

			var info = _orderInfo.TryGetValue(trade.OrderId);

			if (info == null || info.Second <= 0)
				return;

			info.Second -= (decimal)trade.Volume;

			if (info.Second < 0)
				throw new InvalidOperationException(LocalizedStrings.Str3301Params.Put(trade.OrderId, info.Second));

			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Order,
				OrderId = trade.OrderId,
				Balance = info.Second,
				OrderState = info.Second > 0 ? OrderStates.Active : OrderStates.Done
			});
		}

		private void ProcessFunds(IEnumerable<KeyValuePair<string, double>> funds)
		{
			foreach (var fund in funds)
			{
				SendOutMessage(this
					.CreatePortfolioChangeMessage(fund.Key)
						.Add(PositionChangeTypes.CurrentValue, (decimal)fund.Value));
			}
		}

		private void ProcessOrderStatus()
		{
			if (_requestOrderFirst)
			{
				_requestOrderFirst = false;

				var orders = _client.GetOrders().Items.Values;

				foreach (var o in orders)
				{
					var order = o;
					_orderInfo.SafeAdd(order.Id, key => RefTuple.Create(TransactionIdGenerator.GetNextId(), (decimal)order.Volume));
				}

				var trades = _client.GetMyTrades(0).Items.Values;

				foreach (var trade in trades.OrderBy(t => t.Id))
				{
					var info = _orderInfo.TryGetValue(trade.OrderId);

					if (info == null)
						continue;

					info.Second -= (decimal)trade.Volume;
				}

				_hasActiveOrders = false;

				foreach (var order in orders)
				{
					_hasActiveOrders = true;
					ProcessOrder(order);
				}

				_hasMyTrades = false;

				foreach (var trade in trades)
				{
					ProcessExecution(trade);
				}

				return;
			}

			if (_hasMyTrades)
			{
				var mtReply = _client.GetMyTrades(_lastMyTradeId + 1);

				_hasMyTrades = false;

				foreach (var trade in mtReply.Items.Values.OrderBy(t => t.Id))
				{
					ProcessExecution(trade);
				}
			}

			if (_hasActiveOrders)
			{
				var orderReply = _client.GetOrders();

				_hasActiveOrders = false;

				foreach (var order in orderReply.Items.Values)
				{
					_hasActiveOrders = true;
					ProcessOrder(order);
				}
			}
		}

		private void ProcessPortfolioLookup(PortfolioLookupMessage message)
		{
			var reply = _client.GetInfo();
			ProcessFunds(reply.State.Funds);

			SendOutMessage(new PortfolioMessage
			{
				PortfolioName = GetPortfolioName(),
				State = reply.State.Rights.CanTrade ? PortfolioStates.Active : PortfolioStates.Blocked,
				OriginalTransactionId = message == null ? 0 : message.TransactionId
			});

			if (message != null)
				SendOutMessage(new PortfolioLookupResultMessage { OriginalTransactionId = message.TransactionId });
		}
	}
}