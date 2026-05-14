using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FinancyApplication
{
	// Row VM bound to the account list
	public class AccountRow
	{
		public int AccountID { get; set; }
		public string Name { get; set; }
		public string AccountType { get; set; }
		public decimal Balance { get; set; }
		public string Currency { get; set; }
		public string CurrencySymbol { get; set; }

		public string BalanceDisplay
		{
			get
			{
				string sym = string.IsNullOrEmpty(CurrencySymbol) ? "" : CurrencySymbol;
				return sym + Balance.ToString("N2");
			}
		}
	}

	public partial class AccountsView : UserControl
	{
		private readonly Data db = new Data();
		private readonly User _currentUser;
		private List<Account> _accounts = new List<Account>();

		public AccountsView(User user)
		{
			InitializeComponent();
			_currentUser = user;
			Loaded += (s, e) => LoadAccounts();
		}

		private void LoadAccounts()
		{
			try
			{
				_accounts = db.GetAccountsByUser(_currentUser.UserID);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Could not load accounts: " + ex.Message);
				_accounts = new List<Account>();
			}

			List<AccountRow> rows = new List<AccountRow>();
			foreach (Account a in _accounts)
			{
				rows.Add(new AccountRow
				{
					AccountID = a.AccountID,
					Name = a.Name,
					AccountType = a.AccountType,
					Balance = a.Balance,
					Currency = a.Currency,
					CurrencySymbol = a.CurrencySymbol
				});
			}

			AccountList.ItemsSource = rows;
			EmptyState.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		private void AddAccount_Click(object sender, RoutedEventArgs e)
		{
			ShowAccountForm(null);
		}

		private void EditAccount_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button b || b.Tag == null) return;
			int id = (int)b.Tag;
			Account acc = _accounts.Find(a => a.AccountID == id);
			if (acc == null) return;
			ShowAccountForm(acc);
		}

		private void DeleteAccount_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button b || b.Tag == null) return;
			int id = (int)b.Tag;
			Account acc = _accounts.Find(a => a.AccountID == id);
			if (acc == null) return;

			MessageBoxResult confirm = MessageBox.Show(
				"Delete account \"" + acc.Name + "\"?\nIts transactions may also be affected.",
				"Confirm Delete",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);
			if (confirm != MessageBoxResult.Yes) return;

			acc.Delete();
			LoadAccounts();
		}

		private void ShowAccountForm(Account existing)
		{
			AccountDialog dlg = new AccountDialog(_currentUser, existing);
			dlg.Closed += didSave =>
			{
				ModalContent.Content = null;
				ModalHost.Visibility = Visibility.Collapsed;
				if (didSave) LoadAccounts();
			};
			ModalContent.Content = dlg;
			ModalHost.Visibility = Visibility.Visible;
		}
	}
}
