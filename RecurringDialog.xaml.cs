using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FinancyApplication
{
	public partial class RecurringDialog : UserControl
	{
		private readonly Data db = new Data();
		private readonly User _currentUser;
		private List<Category> _allCategories = new List<Category>();

		public event Action<bool> Closed;

		public RecurringDialog(User currentUser)
		{
			InitializeComponent();
			_currentUser = currentUser;
			Loaded += RecurringDialog_Loaded;
		}

		private void RecurringDialog_Loaded(object sender, RoutedEventArgs e)
		{
			PopulateAccounts();
			_allCategories = db.GetCategoriesByUser(_currentUser.UserID);
			PopulateCategoriesForType(GetSelectedType());

			StartDatePicker.SelectedDate = DateTime.Today;
		}

		private string GetSelectedType()
		{
			ComboBoxItem item = TypeComboBox.SelectedItem as ComboBoxItem;
			return item?.Content?.ToString() ?? "Expense";
		}

		private void PopulateAccounts()
		{
			AccountComboBox.Items.Clear();
			List<Account> accounts = db.GetAccountsByUser(_currentUser.UserID);
			foreach (var a in accounts)
			{
				AccountComboBox.Items.Add(new ComboBoxItem
				{
					Content = a.Name,
					Tag = a.AccountID
				});
			}
			if (AccountComboBox.Items.Count > 0)
				AccountComboBox.SelectedIndex = 0;
		}

		private void PopulateCategoriesForType(string type)
		{
			if (CategoryComboBox == null) return;

			CategoryComboBox.Items.Clear();
			IEnumerable<Category> filtered = _allCategories
				.Where(c => string.Equals(c.Type, type, StringComparison.OrdinalIgnoreCase))
				.OrderBy(c => c.Name);

			foreach (var c in filtered)
			{
				CategoryComboBox.Items.Add(new ComboBoxItem
				{
					Content = c.Name,
					Tag = c.CategoryID
				});
			}

			if (CategoryComboBox.Items.Count > 0)
				CategoryComboBox.SelectedIndex = 0;
		}

		private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			PopulateCategoriesForType(GetSelectedType());
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			if (!decimal.TryParse(AmountInput.Text.Trim(),
				System.Globalization.NumberStyles.Number,
				System.Globalization.CultureInfo.InvariantCulture,
				out decimal amount) || amount <= 0)
			{
				MessageBox.Show("Please enter a valid amount greater than 0.",
					"Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (AccountComboBox.SelectedItem == null)
			{
				MessageBox.Show("You don't have any accounts yet — add one from the Accounts page first.",
					"Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (CategoryComboBox.SelectedItem == null)
			{
				MessageBox.Show("Please pick a category.",
					"Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (FrequencyComboBox.SelectedItem == null)
			{
				MessageBox.Show("Please pick a frequency.",
					"Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			int accountId = (int)(AccountComboBox.SelectedItem as ComboBoxItem).Tag;
			int categoryId = (int)(CategoryComboBox.SelectedItem as ComboBoxItem).Tag;
			string type = GetSelectedType();
			string frequency = (FrequencyComboBox.SelectedItem as ComboBoxItem).Content.ToString();
			DateTime start = StartDatePicker.SelectedDate ?? DateTime.Today;

			try
			{
				RecurringTransaction rt = new RecurringTransaction(
					recurringId: 0,
					accountId: accountId,
					categoryId: categoryId,
					type: type,
					amount: amount,
					frequency: frequency,
					startDate: start);
				rt.Create();

				Closed?.Invoke(true);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Could not save recurring transaction: " + ex.Message);
			}
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			Closed?.Invoke(false);
		}
	}
}
