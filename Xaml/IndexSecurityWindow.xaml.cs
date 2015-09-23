namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Xaml;
	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	/// <summary>
	/// The window for editing <see cref="ExpressionIndexSecurity"/>.
	/// </summary>
	public partial class IndexSecurityWindow : ISecurityWindow
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IndexSecurityWindow"/>.
		/// </summary>
		public IndexSecurityWindow()
		{
			InitializeComponent();
			IndexSecurity = new ExpressionIndexSecurity();
		}

		private Func<string, string> _validateId = id => null;

		/// <summary>
		/// The handler checking the entered identifier availability for <see cref="Security"/>.
		/// </summary>
		public Func<string, string> ValidateId
		{
			get { return _validateId; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_validateId = value;
			}
		}

		Security ISecurityWindow.Security
		{
			get { return IndexSecurity; }
			set { IndexSecurity = (ExpressionIndexSecurity)value; }
		}

		/// <summary>
		/// All available instruments.
		/// </summary>
		public IList<Security> Securities
		{
			get { return IndexEditor.Securities; }
		}

		/// <summary>
		/// Index.
		/// </summary>
		public ExpressionIndexSecurity IndexSecurity
		{
			get { return (ExpressionIndexSecurity)SecurityId.Security; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				SecurityId.Security = value;
				IndexEditor.Text = value.Expression;
				Title += " " + value.Id;
			}
		}

		private void SecurityId_TextChanged(object sender, TextChangedEventArgs e)
		{
			ValidateOk();
		}

		private void IndexEditor_TextChanged(object sender, TextChangedEventArgs e)
		{
			ValidateOk();
		}

		private void ValidateOk()
		{
			OK.IsEnabled = !SecurityId.Text.IsEmpty() && !IndexEditor.Text.IsEmpty();
		}

		private void OK_Click(object sender, RoutedEventArgs e)
		{
			var exp = IndexEditor.Expression;

			var mbBuilder = new MessageBoxBuilder()
				.Owner(this)
				.Error();

			if (exp.HasErrors())
			{
				mbBuilder.Text(LocalizedStrings.Str1523Params.Put(exp.Error)).Show();
				return;
			}

			if (IndexSecurity.Id.IsEmpty())
			{
				var errorMsg = _validateId(SecurityId.Text) ?? SecurityId.ValidateId();
				if (!errorMsg.IsEmpty())
				{
					mbBuilder.Text(errorMsg).Show();
					return;
				}

				IndexSecurity.Id = SecurityId.Text;
			}

			IndexSecurity.Expression = IndexEditor.Text;

			DialogResult = true;
		}
	}
}