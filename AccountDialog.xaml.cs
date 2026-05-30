using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FinancyApplication
{
	public partial class AccountDialog : UserControl
	{
		private readonly User _currentUser;
		private readonly Account _existing; 

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
				// Add mode: populate currencies and preselect USD
				List<string> currencies = Account.GetCurrencies();
				foreach (string c in currencies)
				{
					CurrencyCombo.Items.Add(c);
				}

				int usdIdx = currencies.FindIndex(c => c.StartsWith("USD"));
				if (usdIdx >= 0)
				{
					CurrencyCombo.SelectedIndex = usdIdx;
				}
				else
				{
					CurrencyCombo.SelectedIndex = 0;
				}
			}
			else
			{
				// Edit mode: only name is editable
				DialogTitle.Text = "Rename Account";
				SaveButton.Content = "Save";
				NameInput.Text = _existing.Name;
				AddOnlyFields.Visibility = Visibility.Collapsed;
			}
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			string name = "";
			if (NameInput.Text != null)
			{
				name = NameInput.Text.Trim();
			}

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
				if (Closed != null)
				{
					Closed(true);
				}
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
			string accountType = "Checking";
			if (typeItem != null && typeItem.Content != null)
			{
				accountType = typeItem.Content.ToString();
			}

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
				if (Closed != null)
				{
					Closed(true);
				}
			}
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			if (Closed != null)
			{
				Closed(false);
			}
		}
	}
}
