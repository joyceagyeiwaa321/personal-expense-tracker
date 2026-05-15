using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FinancyApplication
{
	// Row VM for the transactions DataGrid
	public class TransactionRow
	{
		public int TransactionID { get; set; }
		public string Description { get; set; }
		public string CategoryName { get; set; }
		public int CategoryID { get; set; }
		public string AccountName { get; set; }
		public int AccountID { get; set; }
		public string Type { get; set; }
		public DateTime Date { get; set; }
		public decimal Amount { get; set; }

		public string DateDisplay => Date.ToString("MMM d, yyyy");
		public string AmountDisplay => (Type == "Income" ? "+" : "-") + Amount.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
		public Brush AmountColor => Type == "Income"
			? new SolidColorBrush(Color.FromRgb(0, 184, 148))
			: new SolidColorBrush(Color.FromRgb(31, 41, 55));
		public Brush TypeChipBg => Type == "Income"
			? new SolidColorBrush(Color.FromRgb(220, 252, 231))
			: new SolidColorBrush(Color.FromRgb(254, 226, 226));
		public Brush TypeChipFg => Type == "Income"
			? new SolidColorBrush(Color.FromRgb(22, 101, 52))
			: new SolidColorBrush(Color.FromRgb(153, 27, 27));
	}

	// Row VM for the recurring list
	public class RecurringRow
	{
		public int RecurringId { get; set; }
		public string Title { get; set; }
		public string Frequency { get; set; }
		public bool IsActive { get; set; }
		public decimal Amount { get; set; }
		public string Type { get; set; }
		public DateTime NextRun { get; set; }

		public string AmountDisplay => (Type == "Income" ? "+" : "") + Amount.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
		public string NextRunDisplay => "Next: " + NextRun.ToString("MMM d, yyyy");
		public string StatusText => IsActive ? "Active" : "Paused";
		public string ToggleLabel => IsActive ? "Pause" : "Resume";
		public Brush StatusBg => IsActive
			? new SolidColorBrush(Color.FromRgb(220, 252, 231))
			: new SolidColorBrush(Color.FromRgb(241, 245, 249));
		public Brush StatusFg => IsActive
			? new SolidColorBrush(Color.FromRgb(22, 101, 52))
			: new SolidColorBrush(Color.FromRgb(71, 85, 105));
	}

	public partial class TransactionsView : UserControl
	{
		private readonly Data db = new Data();
		private readonly User _currentUser;

		private List<Transaction> _allTransactions = new List<Transaction>();
		private Dictionary<int, string> _categoryNames = new Dictionary<int, string>();
		private Dictionary<int, string> _accountNames = new Dictionary<int, string>();

		public TransactionsView(User user)
		{
			InitializeComponent();
			_currentUser = user;
			Loaded += (s, e) => LoadAll();
		}

		// ── LOAD ────────────────────────────────────────────────────────────

		private void LoadAll()
		{
			// Build name lookups so the grid can show readable text
			_categoryNames = db.GetCategoriesByUser(_currentUser.UserID)
				.ToDictionary(c => c.CategoryID, c => c.Name);
			_accountNames = db.GetAccountsByUser(_currentUser.UserID)
				.ToDictionary(a => a.AccountID, a => a.Name);

			PopulateFilterDropdowns();
			LoadTransactions();
			LoadRecurring();
		}

		private void PopulateFilterDropdowns()
		{
			CategoryFilter.Items.Clear();
			CategoryFilter.Items.Add(new ComboBoxItem { Content = "All Categories", Tag = 0 });
			foreach (var kv in _categoryNames)
				CategoryFilter.Items.Add(new ComboBoxItem { Content = kv.Value, Tag = kv.Key });
			CategoryFilter.SelectedIndex = 0;

			AccountFilter.Items.Clear();
			AccountFilter.Items.Add(new ComboBoxItem { Content = "All Accounts", Tag = 0 });
			foreach (var kv in _accountNames)
				AccountFilter.Items.Add(new ComboBoxItem { Content = kv.Value, Tag = kv.Key });
			AccountFilter.SelectedIndex = 0;
		}

		private void LoadTransactions()
		{
			try
			{
				_allTransactions = db.GetTransactionsByUser(_currentUser.UserID);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Could not load transactions: " + ex.Message);
				_allTransactions = new List<Transaction>();
			}
			ApplyFilters();
		}

		private void LoadRecurring()
		{
			try
			{
				List<RecurringTransaction> recurring = db.GetRecurringByUser(_currentUser.UserID);
				List<RecurringRow> rows = new List<RecurringRow>();

				foreach (var r in recurring)
				{
					string catName;
					if (_categoryNames.ContainsKey(r.CategoryId))
						catName = _categoryNames[r.CategoryId];
					else
						catName = "Untitled";

					rows.Add(new RecurringRow
					{
						RecurringId = r.RecurringId,
						Title = catName,
						Frequency = r.Frequency,
						IsActive = r.IsActive,
						Amount = r.Amount,
						Type = r.Type,
						NextRun = r.NextRunDate
					});
				}

				RecurringList.ItemsSource = rows;
				RecurringEmpty.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Could not load recurring: " + ex.Message);
			}
		}

		// ── FILTER ──────────────────────────────────────────────────────────

		private void ApplyFilters()
		{
			// Guard against the SelectionChanged / TextChanged events that fire
			// during InitializeComponent (before the DataGrid + footer exist).
			if (TransactionsGrid == null || FooterCount == null || EmptyState == null)
				return;

			IEnumerable<Transaction> q = _allTransactions;

			// Search
			string search = SearchInput?.Text?.Trim() ?? "";
			if (!string.IsNullOrEmpty(search))
			{
				q = q.Where(t => t.Description != null &&
					t.Description.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
			}

			// Type
			if (TypeFilter?.SelectedItem is ComboBoxItem typeItem)
			{
				string typeVal = typeItem.Content.ToString();
				if (typeVal != "All Types")
					q = q.Where(t => string.Equals(t.Type, typeVal, StringComparison.OrdinalIgnoreCase));
			}

			// Category
			if (CategoryFilter?.SelectedItem is ComboBoxItem catItem && (int)catItem.Tag != 0)
			{
				int catId = (int)catItem.Tag;
				q = q.Where(t => t.CategoryID == catId);
			}

			// Account
			if (AccountFilter?.SelectedItem is ComboBoxItem accItem && (int)accItem.Tag != 0)
			{
				int accId = (int)accItem.Tag;
				q = q.Where(t => t.AccountID == accId);
			}

			// Period
			DateTime now = DateTime.Today;
			if (PeriodFilter?.SelectedItem is ComboBoxItem periodItem)
			{
				string p = periodItem.Content.ToString();
				if (p == "This Month")
				{
					q = q.Where(t => t.Date.Year == now.Year && t.Date.Month == now.Month);
				}
				else if (p == "Last Month")
				{
					DateTime lm = now.AddMonths(-1);
					q = q.Where(t => t.Date.Year == lm.Year && t.Date.Month == lm.Month);
				}
				else if (p == "Last 3 Months")
				{
					DateTime cutoff = now.AddMonths(-3);
					q = q.Where(t => t.Date >= cutoff);
				}
				// "All Time" — no filter
			}

			// Sort
			string sort = "Newest first";
			if (SortFilter?.SelectedItem is ComboBoxItem sortItem)
				sort = sortItem.Content.ToString();

			IOrderedEnumerable<Transaction> sorted = sort switch
			{
				"Oldest first" => q.OrderBy(t => t.Date),
				"Highest amount" => q.OrderByDescending(t => t.Amount),
				"Lowest amount" => q.OrderBy(t => t.Amount),
				_ => q.OrderByDescending(t => t.Date), // Newest first (default)
			};

			List<TransactionRow> rows = sorted
				.Select(t => new TransactionRow
				{
					TransactionID = t.TransactionID,
					Description = t.Description,
					CategoryID = t.CategoryID,
					CategoryName = _categoryNames.ContainsKey(t.CategoryID) ? _categoryNames[t.CategoryID] : "Uncategorized",
					AccountID = t.AccountID,
					AccountName = _accountNames.ContainsKey(t.AccountID) ? _accountNames[t.AccountID] : "—",
					Type = t.Type,
					Date = t.Date,
					Amount = t.Amount
				})
				.ToList();

			TransactionsGrid.ItemsSource = rows;
			FooterCount.Text = "Showing " + rows.Count + " transaction" + (rows.Count == 1 ? "" : "s");
			EmptyState.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilters();

		private void ClearFilters_Click(object sender, RoutedEventArgs e)
		{
			SearchInput.Text = "";
			TypeFilter.SelectedIndex = 0;
			CategoryFilter.SelectedIndex = 0;
			AccountFilter.SelectedIndex = 0;
			PeriodFilter.SelectedIndex = 0;
			SortFilter.SelectedIndex = 0;
			ApplyFilters();
		}

		// ── TABS ────────────────────────────────────────────────────────────

		private void TabAll_Click(object sender, RoutedEventArgs e)
		{
			AllPanel.Visibility = Visibility.Visible;
			RecurringPanel.Visibility = Visibility.Collapsed;
			TabAll.Foreground = new SolidColorBrush(Color.FromRgb(0, 184, 148));
			TabRecurring.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));
		}

		private void TabRecurring_Click(object sender, RoutedEventArgs e)
		{
			AllPanel.Visibility = Visibility.Collapsed;
			RecurringPanel.Visibility = Visibility.Visible;
			TabAll.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));
			TabRecurring.Foreground = new SolidColorBrush(Color.FromRgb(0, 184, 148));
			LoadRecurring();
		}

		// ── ADD / EDIT / DELETE ─────────────────────────────────────────────

		private void AddTransaction_Click(object sender, RoutedEventArgs e)
		{
			ShowTransactionForm(null);
		}

		private void EditTransaction_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button b || b.Tag == null) return;
			int id = (int)b.Tag;
			Transaction t = _allTransactions.Find(x => x.TransactionID == id);
			if (t == null) return;

			ShowTransactionForm(t);
		}

		// Embeds the TransactionDialog UserControl into the modal overlay grid
		// rather than opening it as a popup Window.
		private void ShowTransactionForm(Transaction existing)
		{
			TransactionDialog dlg = new TransactionDialog(_currentUser, existing);
			dlg.Closed += didSave =>
			{
				ModalContent.Content = null;
				ModalHost.Visibility = Visibility.Collapsed;
				if (didSave) LoadTransactions();
			};
			ModalContent.Content = dlg;
			ModalHost.Visibility = Visibility.Visible;
		}

		// Same modal pattern, but the form lets the user create a recurring entry.
		private void AddRecurring_Click(object sender, RoutedEventArgs e)
		{
			RecurringDialog dlg = new RecurringDialog(_currentUser);
			dlg.Closed += didSave =>
			{
				ModalContent.Content = null;
				ModalHost.Visibility = Visibility.Collapsed;
				if (didSave) LoadRecurring();
			};
			ModalContent.Content = dlg;
			ModalHost.Visibility = Visibility.Visible;
		}

		private void DeleteTransaction_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button b || b.Tag == null) return;
			int id = (int)b.Tag;
			Transaction t = _allTransactions.Find(x => x.TransactionID == id);
			if (t == null) return;

			MessageBoxResult res = MessageBox.Show(
				"Delete this transaction?\n\n\"" + t.Description + "\" — " + t.Amount.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-US")),
				"Confirm delete",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (res == MessageBoxResult.Yes)
			{
				t.Delete();
				LoadTransactions();
			}
		}

		// ── RECURRING ACTIONS ───────────────────────────────────────────────

		private void RecurringToggle_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button b || b.Tag == null) return;
			int id = (int)b.Tag;

			try
			{
				List<RecurringTransaction> all = db.GetRecurringByUser(_currentUser.UserID);
				RecurringTransaction rt = all.Find(x => x.RecurringId == id);
				if (rt == null) return;

				rt.IsActive = !rt.IsActive;
				db.UpdateRecurringTransaction(rt);
				LoadRecurring();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Could not update recurring: " + ex.Message);
			}
		}

		private void RecurringDelete_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button b || b.Tag == null) return;
			int id = (int)b.Tag;

			MessageBoxResult res = MessageBox.Show(
				"Delete this recurring transaction?",
				"Confirm delete",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (res == MessageBoxResult.Yes)
			{
				try
				{
					db.DeleteRecurringTransaction(id);
					LoadRecurring();
				}
				catch (Exception ex)
				{
					MessageBox.Show("Could not delete recurring: " + ex.Message);
				}
			}
		}
	}
}
