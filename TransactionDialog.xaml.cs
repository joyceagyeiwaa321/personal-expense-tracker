using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FinancyApplication
{
    public partial class TransactionDialog : UserControl
    {
        private readonly Data db = new Data();
        private readonly User _currentUser;
        private readonly Transaction _existing; 

        private List<Category> _allCategories = new List<Category>();
        private string _selectedReceiptPath = null;

        // fires when form closes
        public event Action<bool> Closed;

        public TransactionDialog(User currentUser, Transaction existing)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _existing = existing;

            Loaded += TransactionDialog_Loaded;
        }

        private void TransactionDialog_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateAccounts();
            _allCategories = db.GetCategoriesByUser(_currentUser.UserID);
            PopulateCategoriesForType(GetSelectedType());

            if (_existing == null)
            {
                DatePickerInput.SelectedDate = DateTime.Today;
            }
            else
            {
                // Edit mode
                DialogTitle.Text = "Edit Transaction";
                SaveButton.Content = "Update Transaction";

                foreach (ComboBoxItem item in TypeComboBox.Items)
                {
                    if (item.Content.ToString().Equals(_existing.Type, StringComparison.OrdinalIgnoreCase))
                    {
                        TypeComboBox.SelectedItem = item;
                        break;
                    }
                }

                SelectComboByTag(AccountComboBox, _existing.AccountID);
                SelectComboByTag(CategoryComboBox, _existing.CategoryID);

                AmountInput.Text = _existing.Amount.ToString("0.##");
                DescriptionInput.Text = _existing.Description;
                DatePickerInput.SelectedDate = _existing.Date;
            }
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
            // just in case
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
            // When the user switches Income ↔ Expense, reload the matching categories.
            PopulateCategoriesForType(GetSelectedType());
        }

        private static void SelectComboByTag(ComboBox combo, int tagValue)
        {
            foreach (ComboBoxItem item in combo.Items)
            {
                if (item.Tag != null && (int)item.Tag == tagValue)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validate amount
            if (!decimal.TryParse(AmountInput.Text.Trim(),
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Please enter a valid amount greater than 0.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(DescriptionInput.Text))
            {
                MessageBox.Show("Please enter a description.",
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

            int accountId = (int)(AccountComboBox.SelectedItem as ComboBoxItem).Tag;
            int categoryId = (int)(CategoryComboBox.SelectedItem as ComboBoxItem).Tag;
            string type = GetSelectedType();
            DateTime date = DatePickerInput.SelectedDate ?? DateTime.Today;
            string description = DescriptionInput.Text.Trim();

            try
            {
                int savedTransactionId = 0;

                if (_existing == null)
                {
                    Transaction t = new Transaction
                    {
                        UserID = _currentUser.UserID,
                        AccountID = accountId,
                        CategoryID = categoryId,
                        Type = type,
                        Amount = amount,
                        Description = description,
                        Date = date,
                        GroupID = 0
                    };
                    t.Create();
                    savedTransactionId = t.TransactionID;
                }
                else
                {
                    _existing.AccountID = accountId;
                    _existing.CategoryID = categoryId;
                    _existing.Type = type;
                    _existing.Amount = amount;
                    _existing.Description = description;
                    _existing.Date = date;
                    _existing.Update();
                    savedTransactionId = _existing.TransactionID;
                }

                // Attach receipt if one was selected
                if (!string.IsNullOrEmpty(_selectedReceiptPath) && savedTransactionId > 0)
                {
                    try
                    {
                        string ext = System.IO.Path.GetExtension(_selectedReceiptPath)
                            .ToLowerInvariant().TrimStart('.');
                        var receipt = new Receipt(0, savedTransactionId, _selectedReceiptPath, ext);
                        int receiptId = receipt.Upload();
                        if (receiptId > 0)
                            db.AttachReceiptToTransaction(savedTransactionId, receiptId);
                    }
                    catch {  }
                }

                Closed?.Invoke(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save transaction: " + ex.Message);
            }
        }

        private async void AttachReceipt_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select a receipt",
                Filter = "Image and PDF files (*.jpg;*.jpeg;*.png;*.pdf)|*.jpg;*.jpeg;*.png;*.pdf|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            _selectedReceiptPath = dlg.FileName;
            ReceiptFileLabel.Text = System.IO.Path.GetFileName(dlg.FileName);
            ReceiptFileLabel.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(31, 41, 55));

            // Only scan images — skip PDFs
            string ext = System.IO.Path.GetExtension(dlg.FileName).ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png") return;

            string apiKey = OpenAiService.LoadApiKey();
            if (string.IsNullOrEmpty(apiKey)) return;

            try
            {
                ReceiptFileLabel.Text = "Scanning receipt...";

                var extracted = await OpenAiService.ScanReceipt(dlg.FileName, apiKey);

                // Fill in Amount if empty
                if (!string.IsNullOrEmpty(extracted["Amount"]) &&
                    string.IsNullOrWhiteSpace(AmountInput.Text))
                    AmountInput.Text = extracted["Amount"];

                // Fill in Description if empty
                if (!string.IsNullOrEmpty(extracted["Description"]) &&
                    string.IsNullOrWhiteSpace(DescriptionInput.Text))
                    DescriptionInput.Text = extracted["Description"];

                // Fill in Date if empty
                if (!string.IsNullOrEmpty(extracted["Date"]) &&
                    DateTime.TryParse(extracted["Date"], out DateTime parsedDate))
                    DatePickerInput.SelectedDate = parsedDate;

                ReceiptFileLabel.Text = System.IO.Path.GetFileName(dlg.FileName);
            }
            catch
            {
                ReceiptFileLabel.Text = System.IO.Path.GetFileName(dlg.FileName);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Closed?.Invoke(false);
        }
    }
}