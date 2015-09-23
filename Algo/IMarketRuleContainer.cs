namespace StockSharp.Algo
{
	using System;

	using StockSharp.Logging;

	/// <summary>
	/// ���������, ����������� ��������� ������.
	/// </summary>
	public interface IMarketRuleContainer : ILogReceiver
	{
		/// <summary>
		/// ��������� ������.
		/// </summary>
		ProcessStates ProcessState { get; }

		/// <summary>
		/// ������������ �������.
		/// </summary>
		/// <param name="rule">�������.</param>
		/// <param name="process">����������, ������������ <see langword="true"/>, ���� ������� ��������� ���� ������, ����� - <see langword="false"/>.</param>
		void ActivateRule(IMarketRule rule, Func<bool> process);

		/// <summary>
		/// �������������� �� ���������� ������.
		/// </summary>
		/// <remarks>
		/// ������������ ������ ���������� ����� ����� <see cref="SuspendRules()"/>.
		/// </remarks>
		bool IsRulesSuspended { get; }

		/// <summary>
		/// ������������� ���������� ������ �� ���������� �������������� ����� ����� <see cref="ResumeRules"/>.
		/// </summary>
		void SuspendRules();

		/// <summary>
		/// ������������ ���������� ������, ������������� ����� ����� <see cref="SuspendRules()"/>.
		/// </summary>
		void ResumeRules();

		/// <summary>
		/// ������������������ �������.
		/// </summary>
		IMarketRuleList Rules { get; }
	}
}