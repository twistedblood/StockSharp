namespace StockSharp.Algo.Strategies
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// �������� ���������.
	/// </summary>
	public interface IStrategyParam : IPersistable
	{
		/// <summary>
		/// �������� ���������.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// �������� ���������.
		/// </summary>
		object Value { get; set; }

		/// <summary>
		/// �������� �� ��� �����������.
		/// </summary>
		object OptimizeFrom { get; set; }

		/// <summary>
		/// �������� �� ��� �����������.
		/// </summary>
		object OptimizeTo { get; set; }

		/// <summary>
		/// �������� ��� ��� �����������.
		/// </summary>
		object OptimizeStep { get; set; }
	}

	/// <summary>
	/// ������� ��� ��������������� ������� � ��������� ���������.
	/// </summary>
	/// <typeparam name="T">��� �������� ���������.</typeparam>
	public class StrategyParam<T> : IStrategyParam
	{
		private readonly Strategy _strategy;

		/// <summary>
		/// ������� <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <param name="strategy">���������.</param>
		/// <param name="name">�������� ���������.</param>
		public StrategyParam(Strategy strategy, string name)
			: this(strategy, name, default(T))
		{
		}

		/// <summary>
		/// ������� <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <param name="strategy">���������.</param>
		/// <param name="name">�������� ���������.</param>
		/// <param name="initialValue">�������������� ��������.</param>
		public StrategyParam(Strategy strategy, string name, T initialValue)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			if (name.IsEmpty())
				throw new ArgumentNullException("name");

			_strategy = strategy;
			Name = name;
			_value = initialValue;

			_strategy.Parameters.Add(this);
		}

		/// <summary>
		/// �������� ���������.
		/// </summary>
		public string Name { get; private set; }

		private bool _allowNull = typeof(T).IsNullable();

		/// <summary>
		/// �������� �� � <see cref="Value"/> ������� ��������, ������ <see langword="null"/>.
		/// </summary>
		public bool AllowNull
		{
			get { return _allowNull; }
			set { _allowNull = value; }
		}

		private T _value;

		/// <summary>
		/// �������� ���������.
		/// </summary>
		public T Value
		{
			get
			{
				return _value;
			}
			set
			{
				if (!AllowNull && value.IsNull())
					throw new ArgumentNullException("value");

				if (EqualityComparer<T>.Default.Equals(_value, value))
					return;

				var propChange = _value as INotifyPropertyChanged;
				if (propChange != null)
					propChange.PropertyChanged -= OnValueInnerStateChanged;

				_value = value;
				_strategy.RaiseParametersChanged(Name);

				propChange = _value as INotifyPropertyChanged;
				if (propChange != null)
					propChange.PropertyChanged += OnValueInnerStateChanged;
			}
		}

		/// <summary>
		/// �������� �� ��� �����������.
		/// </summary>
		public object OptimizeFrom { get; set; }

		/// <summary>
		/// �������� �� ��� �����������.
		/// </summary>
		public object OptimizeTo { get; set; }

		/// <summary>
		/// �������� ��� ��� �����������.
		/// </summary>
		public object OptimizeStep { get; set; }

		private void OnValueInnerStateChanged(object sender, PropertyChangedEventArgs e)
		{
			_strategy.RaiseParametersChanged(Name);
		}

		object IStrategyParam.Value
		{
			get { return Value; }
			set { Value = (T)value; }
		}

		/// <summary>
		/// ��������� ���������.
		/// </summary>
		/// <param name="storage">��������� ��������.</param>
		public void Load(SettingsStorage storage)
		{
			Name = storage.GetValue<string>("Name");
			Value = storage.GetValue<T>("Value");
			OptimizeFrom = storage.GetValue<T>("OptimizeFrom");
			OptimizeTo = storage.GetValue<T>("OptimizeTo");
			OptimizeStep = storage.GetValue<object>("OptimizeStep");
		}

		/// <summary>
		/// ��������� ���������.
		/// </summary>
		/// <param name="storage">��������� ��������.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("Name", Name);
			storage.SetValue("Value", Value);
			storage.SetValue("OptimizeFrom", OptimizeFrom);
			storage.SetValue("OptimizeTo", OptimizeTo);
			storage.SetValue("OptimizeStep", OptimizeStep);
		}
	}

	/// <summary>
	/// ��������������� ����� ��� � <see cref="StrategyParam{T}"/>.
	/// </summary>
	public static class StrategyParamHelper
	{
		/// <summary>
		/// ������� <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <typeparam name="T">��� �������� ���������.</typeparam>
		/// <param name="strategy">���������.</param>
		/// <param name="name">�������� ���������.</param>
		/// <param name="initialValue">�������������� ��������.</param>
		/// <returns>�������� ���������.</returns>
		public static StrategyParam<T> Param<T>(this Strategy strategy, string name, T initialValue = default(T))
		{
			return new StrategyParam<T>(strategy, name, initialValue);
		}

		/// <summary>
		/// ������� <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <typeparam name="T">��� �������� ���������.</typeparam>
		/// <param name="param">�������� ���������.</param>
		/// <param name="optimizeFrom">�������� �� ��� �����������.</param>
		/// <param name="optimizeTo">�������� �� ��� �����������.</param>
		/// <param name="optimizeStep">�������� ��� ��� �����������.</param>
		/// <returns>�������� ���������.</returns>
		public static StrategyParam<T> Optimize<T>(this StrategyParam<T> param, T optimizeFrom = default(T), T optimizeTo = default(T), T optimizeStep = default(T))
		{
			if (param == null)
				throw new ArgumentNullException("param");

			param.OptimizeFrom = optimizeFrom;
			param.OptimizeTo = optimizeTo;
			param.OptimizeStep = optimizeStep;

			return param;
		}
	}
}