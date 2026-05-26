using System;
using System.Windows;
using System.Windows.Controls;

namespace FinancyApplication
{
	public partial class PrivacyView : UserControl
	{
		public event EventHandler BackRequested;

		public PrivacyView()
		{
			InitializeComponent();
		}

		private void BackToDashboard_Click(object sender, RoutedEventArgs e)
		{
			if (BackRequested != null)
			{
				BackRequested(this, EventArgs.Empty);
			}
		}
	}
}
