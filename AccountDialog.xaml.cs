using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FinancyApplication
{
	public partial class AccountDialog : UserControl
	{
		private readonly User _currentUser;
		private readonly Account _existing; // null = add mode

		public event Action<bool> Closed;

		public AccountDialog(User currentUser, Account existing)
		{
			InitializeComponent();
			_currentUser = currentUser;
			_existing = existing;

			Loaded += AccountDialog_Loaded;
		}

		private void AccountDialog_Loaded(object sender, RoutedEventArgs e)
		{
			if (_existing == null)
			{
				// Add mode — populate currencies and preselect USD if present.
				List<string> currencies = Account.GetCurrencies();
				foreach (string c in currencies)
					CurrencyCombo.Items.Add(c);

				int usdIdx = currencies.FindIndex(c => c.StartsWith("USD"));
				CurrencyCombo.SelectedIndex = usdIdx >= 0 ? usdIdx : 0;
			}
			else
			{
				// Edit mode — only the name is editable (Account class only exposes Rename).
				DialogTitle.Text = "Rename Account";
				SaveButton.Content = "Save";
				NameInput.Text = _existing.Name;
				AddOnlyFields.Visibility = Visibility.Collapsed;
			}
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			string name = NameInput.Text?.Trim() ?? "";
			if (string.IsNullOrWhiteSpace(name))
			{
				MessageBox.Show("Please enter an account name.",
					"Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (_existing != null)
			{
				// Rename existing account
				_existing.Rename(name);
				Closed?.Invoke(true);
				return;
			}

			// Add mode
			if (!decimal.TryParse(BalanceInput.Text.Trim(),
				System.Globalization.NumberStyles.Number,
				System.Globalization.CultureInfo.InvariantCulture,
				out decimal balance) || balance < 0)
			{
				MessageBox.Show("Initial balance must be a number 0 or greater.",
					"Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			ComboBoxItem typeItem = AccountTypeCombo.SelectedItem as ComboBoxItem;
			string accountType = typeItem?.Content?.ToString() ?? "Checking";

			string currencyDropdownValue = CurrencyCombo.SelectedItem as string;
			if (string.IsNullOrWhiteSpace(currencyDropdownValue))
			{
				MessageBox.Show("Please pick a currency.",
					"Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			Account acc = new Account(_currentUser.UserID, name, accountType, balance, currencyDropdownValue);
			if (acc.Save())
			{
				Closed?.Invoke(true);
			}
			// If Save() failed it already showed a MessageBox; leave the form open.
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			Closed?.Invoke(false);
		}
	}
}
