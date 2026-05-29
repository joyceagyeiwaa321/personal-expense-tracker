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

		//  LOAD

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

		//  FILTER

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

		//  TABS

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

		//  ADD / EDIT / DELETE 

		        //  CSV / XLSX IMPORT 
        //
        // Imports transactions from a bank CSV or Excel file.
        // If the column headers are not recognized, the AI maps them automatically.
        // Rows without a matching category also get AI-suggested categories.
        private async void ImportTransactions_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null) return;

            // Step 1: Let the user pick a file
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Choose a CSV or Excel file to import",
                Filter = "CSV files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx|All supported|*.csv;*.xlsx"
            };
            if (dlg.ShowDialog() != true) return;

            // Step 2: Read the file into a list of string arrays (one per row)
            List<string[]> rows;
            try
            {
                string ext = System.IO.Path.GetExtension(dlg.FileName).ToLowerInvariant();
                rows = ext == ".xlsx" ? ReadXlsx(dlg.FileName) : ReadCsv(dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Couldn't read the file: " + ex.Message, "Import failed");
                return;
            }

            if (rows == null || rows.Count < 2)
            {
                MessageBox.Show("File looks empty or has no data rows.", "Import");
                return;
            }

            // Step 3: Try to find column positions from the header row using known names
            string[] header = rows[0];
            int iDate     = FindCol(header, "date", "started date", "completed date", "transaction date", "booking date", "value date");
            int iDesc     = FindCol(header, "description", "desc", "name", "memo");
            int iAmount   = FindCol(header, "amount", "value");
            int iType     = FindCol(header, "type");
            int iCategory = FindCol(header, "category");
            int iAccount  = FindCol(header, "account");

            // Step 4: If required columns are still missing, ask AI to map them
            if (iDate < 0 || iAmount < 0)
            {
                string apiKey = OpenAiService.LoadApiKey();

                if (string.IsNullOrEmpty(apiKey))
                {
                    MessageBox.Show(
                        "Could not find the required columns (Date, Amount) in this file.\n\n" +
                        "To let AI map columns automatically, add your OpenAI API key in Profile → AI Settings.",
                        "Import — columns not found");
                    return;
                }

                try
                {
                    // Send the header row to GPT and get back a column-index mapping
                    Dictionary<string, int> aiMap = await OpenAiService.MapColumns(header, apiKey);

                    // Only fill in columns that manual detection didn't find
                    if (iDate < 0 && aiMap.ContainsKey("Date"))             iDate     = aiMap["Date"];
                    if (iDesc < 0 && aiMap.ContainsKey("Description"))      iDesc     = aiMap["Description"];
                    if (iAmount < 0 && aiMap.ContainsKey("Amount"))         iAmount   = aiMap["Amount"];
                    if (iType < 0 && aiMap.ContainsKey("Type"))             iType     = aiMap["Type"];
                    if (iCategory < 0 && aiMap.ContainsKey("Category"))     iCategory = aiMap["Category"];
                }
                catch (Exception ex)
                {
                    MessageBox.Show("AI column mapping failed: " + ex.Message, "AI Error");
                    return;
                }
            }

            // After both manual + AI attempts, we still need at least Date and Amount
            if (iDate < 0 || iAmount < 0)
            {
                MessageBox.Show(
                    "File needs at least a Date column and an Amount column.\n\n" +
                    "Recognized column names: Date, Description, Amount, Type, Category, Account.",
                    "Import — missing columns");
                return;
            }

            // Show the user something is happening
            FooterCount.Text = "Reading file...";

            // Step 5: Load the user's accounts and categories from the database
            List<Account> accounts = db.GetAccountsByUser(_currentUser.UserID);
            List<Category> cats    = db.GetCategoriesByUser(_currentUser.UserID);

            if (accounts == null || accounts.Count == 0)
            {
                MessageBox.Show("You need at least one Account before importing transactions.", "Import");
                return;
            }

            int defaultAccountId  = accounts[0].AccountID;
            int defaultCategoryId = cats.Count > 0 ? cats[0].CategoryID : 0;

            // Step 6: Parse every data row into a Transaction object.
            // Rows where no category name is matched get flagged for AI categorization.
            var parsedTransactions = new List<Transaction>();
            var needsAiCategoryAt  = new List<int>();     // indexes in parsedTransactions that need AI
            var needsAiDesc        = new List<string>();  // their descriptions (sent to AI together)
            int skipped = 0;
            var errors  = new List<string>();

            for (int r = 1; r < rows.Count; r++)
            {
                string[] cells = rows[r];
                if (cells.Length == 0 || cells.All(string.IsNullOrWhiteSpace)) continue;

                string rawDate = SafeGet(cells, iDate);
                string rawAmt  = SafeGet(cells, iAmount);

                if (string.IsNullOrWhiteSpace(rawDate) || string.IsNullOrWhiteSpace(rawAmt))
                {
                    skipped++;
                    continue;
                }

                if (!TryParseDate(rawDate, out DateTime date))
                {
                    errors.Add("Row " + (r + 1) + ": invalid date '" + rawDate + "'");
                    skipped++;
                    continue;
                }

                if (!TryParseDecimal(rawAmt, out decimal amount))
                {
                    errors.Add("Row " + (r + 1) + ": invalid amount '" + rawAmt + "'");
                    skipped++;
                    continue;
                }

                // Determine Income vs Expense from the Type column, or fall back to the amount sign
                string rawType = SafeGet(cells, iType).Trim();
                string type;
                if (rawType.Equals("Income", StringComparison.OrdinalIgnoreCase))
                    type = "Income";
                else if (rawType.Equals("Expense", StringComparison.OrdinalIgnoreCase))
                    type = "Expense";
                else
                    type = amount < 0 ? "Expense" : "Income";

                // Bank exports often use negative numbers for expenses — we store positive amounts
                amount = Math.Abs(amount);

                string desc = SafeGet(cells, iDesc).Trim();
                if (string.IsNullOrWhiteSpace(desc))
                {
                    string fallback = SafeGet(cells, iType).Trim();
                    desc = string.IsNullOrWhiteSpace(fallback) ? "Imported" : fallback;
                }

                // Try to match the category by name. -1 means no match was found.
                int catId = ResolveByName(cats, SafeGet(cells, iCategory), -1);
                int accId = ResolveAccountByName(accounts, SafeGet(cells, iAccount), defaultAccountId);

                Transaction tx = new Transaction
                {
                    UserID      = _currentUser.UserID,
                    AccountID   = accId,
                    CategoryID  = catId < 0 ? defaultCategoryId : catId,
                    Type        = type,
                    Amount      = amount,
                    Description = desc,
                    Date        = date
                };

                // If no category was matched, add it to the AI queue.
                // We include the type in brackets so GPT has more context (e.g. "Netflix [Expense]")
                if (catId < 0)
                {
                    needsAiCategoryAt.Add(parsedTransactions.Count);
                    needsAiDesc.Add(desc + " [" + type + "]");
                }

                parsedTransactions.Add(tx);
            }

            // Step 7: Use AI to fill in categories for rows that didn't have a match
            FooterCount.Text = "AI is categorizing transactions...";
            int aiCategorized = 0;
            if (needsAiCategoryAt.Count > 0)
            {
                string apiKey = OpenAiService.LoadApiKey();
                if (!string.IsNullOrEmpty(apiKey))
                {
                    try
                    {
                        // Build the list of category names to offer GPT
                        List<string> catNames = new List<string>();
                        foreach (Category c in cats)
                        {
                            catNames.Add(c.Name);
                        }

                        // Send all unmatched descriptions at once (one API call)
                        List<string> suggested = await OpenAiService.SuggestCategories(needsAiDesc, catNames, apiKey);

                        // Apply the AI suggestions back to the correct transactions
                        for (int i = 0; i < needsAiCategoryAt.Count && i < suggested.Count; i++)
                        {
                            int txIndex = needsAiCategoryAt[i];
                            int catId   = ResolveByName(cats, suggested[i], defaultCategoryId);
                            parsedTransactions[txIndex].CategoryID = catId;
                            aiCategorized++;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Non-fatal: rows will just keep the default category
                        MessageBox.Show("AI category suggestion failed: " + ex.Message + "\nUsing default categories.", "AI Warning");
                    }
                }
            }

            // Step 8: Insert all parsed transactions into the database
            FooterCount.Text = "Inserting transactions...";
            int imported = 0;
            foreach (Transaction tx in parsedTransactions)
            {
                try
                {
                    db.InsertTransaction(tx);
                    imported++;
                }
                catch (Exception ex)
                {
                    errors.Add(ex.Message);
                    skipped++;
                }
            }

            // Step 9: Show a summary of what happened
            string summary = "Imported:  " + imported + "\nSkipped:   " + skipped;
            if (aiCategorized > 0)
            {
                summary += "\nAI categorized: " + aiCategorized + " transactions";
            }
            if (errors.Count > 0)
            {
                summary += "\n\nFirst issues:\n  " + string.Join("\n  ", errors.Take(5));
            }
            MessageBox.Show(summary, "Import complete");

            // Refresh the list so the new rows show up immediately
            LoadTransactions();
        }

                //  IMPORT HELPERS

        private static List<string[]> ReadCsv(string path)
        {
            var rows = new List<string[]>();
            foreach (string line in System.IO.File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                // Simple split — bank CSVs rarely contain commas inside quoted fields,
                // but we handle the common quoted case.
                var cells = new List<string>();
                bool inQuote = false;
                var sb = new System.Text.StringBuilder();
                foreach (char c in line)
                {
                    if (c == '"') inQuote = !inQuote;
                    else if (c == ',' && !inQuote) { cells.Add(sb.ToString()); sb.Clear(); }
                    else sb.Append(c);
                }
                cells.Add(sb.ToString());
                rows.Add(cells.Select(s => s.Trim()).ToArray());
            }
            return rows;
        }

        private static List<string[]> ReadXlsx(string path)
        {
            var rows = new List<string[]>();
            using (var wb = new ClosedXML.Excel.XLWorkbook(path))
            {
                var ws = wb.Worksheet(1);
                foreach (var row in ws.RowsUsed())
                {
                    int last = row.LastCellUsed()?.Address.ColumnNumber ?? 0;
                    var cells = new string[last];
                    for (int c = 1; c <= last; c++)
                        cells[c - 1] = row.Cell(c).GetString().Trim();
                    rows.Add(cells);
                }
            }
            return rows;
        }

        private static int FindCol(string[] header, params string[] names)
        {
            for (int i = 0; i < header.Length; i++)
            {
                string h = (header[i] ?? "").Trim().ToLowerInvariant();
                foreach (string n in names)
                    if (h == n) return i;
            }
            return -1;
        }

        private static string SafeGet(string[] cells, int index)
        {
            if (index < 0 || index >= cells.Length) return "";
            return cells[index] ?? "";
        }

        private static bool TryParseDate(string s, out DateTime date)
        {
            string[] formats = { "yyyy-MM-dd", "yyyy/MM/dd", "dd/MM/yyyy", "MM/dd/yyyy", "dd-MM-yyyy", "d/M/yyyy" };
            if (DateTime.TryParseExact(s, formats, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out date)) return true;
            return DateTime.TryParse(s, out date);
        }

        private static bool TryParseDecimal(string s, out decimal value)
        {
            s = (s ?? "").Replace("€", "").Replace("$", "").Replace(" ", "").Trim();
            // Handle European decimals (comma)
            if (s.Contains(",") && !s.Contains("."))
                s = s.Replace(",", ".");
            return decimal.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        private static int ResolveByName(List<Category> cats, string name, int fallback)
        {
            if (string.IsNullOrWhiteSpace(name)) return fallback;
            var match = cats.FirstOrDefault(c =>
                string.Equals(c.Name?.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase));
            return match != null ? match.CategoryID : fallback;
        }

        private static int ResolveAccountByName(List<Account> accs, string name, int fallback)
        {
            if (string.IsNullOrWhiteSpace(name)) return fallback;
            var match = accs.FirstOrDefault(a =>
                string.Equals(a.Name?.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase));
            return match != null ? match.AccountID : fallback;
        }

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

		//  RECURRING ACTIONS 

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
