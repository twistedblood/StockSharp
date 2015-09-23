﻿namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Ковариация.
	/// </summary>
	/// <remarks>
	/// https://en.wikipedia.org/wiki/Covariance
	/// </remarks>
	[DisplayName("COV")]
	[DescriptionLoc(LocalizedStrings.CovarianceKey, true)]
	public class Covariance : LengthIndicator<Tuple<decimal, decimal>>
	{
		/// <summary>
		/// Создать <see cref="Covariance"/>.
		/// </summary>
		public Covariance()
		{
			Length = 20;
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var value = input.GetValue<Tuple<decimal, decimal>>();

			Buffer.Add(value);

			Tuple<decimal, decimal> first = null;

			if (input.IsFinal)
			{
				if (Buffer.Count > Length)
					Buffer.RemoveAt(0);
			}
			else
			{
				if (Buffer.Count > Length)
				{
					first = Buffer[0];
					Buffer.RemoveAt(0);
				}
			}

			decimal avgSource = 0;
			decimal avgOther = 0;

			foreach (var tuple in Buffer)
			{
				avgSource += tuple.Item1;
				avgOther += tuple.Item2;
			}

			var len = Buffer.Count;

			avgSource /= len;
			avgOther /= len;

			var covariance = 0m;

			foreach (var tuple in Buffer)
			{
				covariance += (tuple.Item1 - avgSource) * (tuple.Item2 - avgOther);
			}

			if (!input.IsFinal)
			{
				if (first != null)
					Buffer.Insert(0, first);

				Buffer.RemoveAt(len - 1);
			}

			return new DecimalIndicatorValue(this, covariance / len);
		}
	}
}