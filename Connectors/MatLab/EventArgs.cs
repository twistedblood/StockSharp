namespace StockSharp.MatLab
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Argument which contains an error information.
	/// </summary>
	public class ErrorEventArgs : EventArgs
	{
		internal ErrorEventArgs(Exception error)
		{
			if (error == null)
				throw new ArgumentNullException("error");

			Error = error;
		}

		/// <summary>
		/// Error info.
		/// </summary>
		public Exception Error { get; private set; }
	}

	/// <summary>
	/// Argument which contains instruments.
	/// </summary>
	public class SecuritiesEventArgs : EventArgs
	{
		internal SecuritiesEventArgs(IEnumerable<Security> securities)
		{
			if (securities == null)
				throw new ArgumentNullException("securities");

			Securities = securities.ToArray();
		}

		/// <summary>
		/// Securities.
		/// </summary>
		public Security[] Securities { get; private set; }
	}

	/// <summary>
	/// Argument which contains orders.
	/// </summary>
	public class OrdersEventArgs : EventArgs
	{
		internal OrdersEventArgs(IEnumerable<Order> orders)
		{
			if (orders == null)
				throw new ArgumentNullException("orders");

			Orders = orders.ToArray();
		}

		/// <summary>
		/// Orders.
		/// </summary>
		public Order[] Orders { get; private set; }
	}

	/// <summary>
	/// Argument which contains order errors (registration or cancellation).
	/// </summary>
	public class OrderFailsEventArgs : EventArgs
	{
		internal OrderFailsEventArgs(IEnumerable<OrderFail> orderFails)
		{
			if (orderFails == null)
				throw new ArgumentNullException("orderFails");

			OrderFails = orderFails.ToArray();
		}

		/// <summary>
		/// Errors.
		/// </summary>
		public OrderFail[] OrderFails { get; private set; }
	}

	/// <summary>
	/// Argument which contains tick teades.
	/// </summary>
	public class TradesEventArgs : EventArgs
	{
		internal TradesEventArgs(IEnumerable<Trade> trades)
		{
			if (trades == null)
				throw new ArgumentNullException("trades");

			Trades = trades.ToArray();
		}

		/// <summary>
		/// Trades.
		/// </summary>
		public Trade[] Trades { get; private set; }
	}

	/// <summary>
	/// Argument which contains own trades.
	/// </summary>
	public class MyTradesEventArgs : EventArgs
	{
		internal MyTradesEventArgs(IEnumerable<MyTrade> trades)
		{
			if (trades == null)
				throw new ArgumentNullException("trades");

			Trades = trades.ToArray();
		}

		/// <summary>
		/// Trades.
		/// </summary>
		public MyTrade[] Trades { get; private set; }
	}

	/// <summary>
	/// Argument which contains portfolios.
	/// </summary>
	public class PortfoliosEventArgs : EventArgs
	{
		internal PortfoliosEventArgs(IEnumerable<Portfolio> portfolios)
		{
			if (portfolios == null)
				throw new ArgumentNullException("portfolios");

			Portfolios = portfolios.ToArray();
		}

		/// <summary>
		/// Portfolios.
		/// </summary>
		public Portfolio[] Portfolios { get; private set; }
	}

	/// <summary>
	/// Argument which contains positions.
	/// </summary>
	public class PositionsEventArgs : EventArgs
	{
		internal PositionsEventArgs(IEnumerable<Position> positions)
		{
			if (positions == null)
				throw new ArgumentNullException("positions");

			Positions = positions.ToArray();
		}

		/// <summary>
		/// Positions.
		/// </summary>
		public Position[] Positions { get; private set; }
	}

	/// <summary>
	/// Argument which contains order books.
	/// </summary>
	public class MarketDepthsEventArgs : EventArgs
	{
		internal MarketDepthsEventArgs(IEnumerable<MarketDepth> depths)
		{
			if (depths == null)
				throw new ArgumentNullException("depths");

			Depths = depths.ToArray();
		}

		/// <summary>
		/// Market depths.
		/// </summary>
		public MarketDepth[] Depths { get; private set; }
	}

	/// <summary>
	/// Argument which contains order logs.
	/// </summary>
	public class OrderLogItemsEventArg : EventArgs
	{
		internal OrderLogItemsEventArg(IEnumerable<OrderLogItem> items)
		{
			if (items == null)
				throw new ArgumentNullException("items");

			Items = items.ToArray();
		}

		/// <summary>
		/// Order log.
		/// </summary>
		public OrderLogItem[] Items { get; private set; }
	}
}