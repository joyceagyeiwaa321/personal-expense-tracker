using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FinancyApplication
{
    public partial class BudgetDialog : Window
    {
        private readonly Data db = new Data();
        private readonly User _currentUser;
        private readonly Dictionary<int, string> _categoryNames;
        private readonly Budget _existingBudget; 
        private readonly string _month;

        public BudgetDialog(User currentUser, Dictionary<int, string> categoryNames,
            Budget existingBudget, string month)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _categoryNames = categoryNames;
            _existingBudget = existingBudget;
            _month = month;

            Loaded += BudgetDialog_Loaded;
        }

        private void BudgetDialog_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateCategories();

            if (_existingBudget != null)
            {
                // Edit mode
                DialogTitle.Text = "Edit Budget";
                SaveButton.Content = "Update Budget";
                LimitInput.Text = _existingBudget.LimitAmount.ToString("N2");

                // Select the matching category
                foreach (ComboBoxItem item in CategoryComboBox.Items)
                {
                    if ((int)item.Tag == _existingBudget.CategoryId)
                    {
                        CategoryComboBox.SelectedItem = item;
                        break;
                    }
                }

                // Lock category in edit mode — user is editing the limit only
                CategoryComboBox.IsEnabled = false;
            }
        }

        private void PopulateCategories()
        {
            CategoryComboBox.Items.Clear();
            foreach (var kv in _categoryNames)
            {
                CategoryComboBox.Items.Add(new ComboBoxItem
                {
                    Content = kv.Value,
                    Tag = kv.Key
                });
            }

            if (CategoryComboBox.Items.Count > 0)
                CategoryComboBox.SelectedIndex = 0;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validate amount
            if (!decimal.TryParse(LimitInput.Text.Trim(), out decimal limit) || limit <= 0)
            {
                MessageBox.Show("Please enter a valid limit amount greater than 0.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int categoryId = (int)(CategoryComboBox.SelectedItem as ComboBoxItem)?.Tag;

            try
            {
                if (_existingBudget == null)
                {
                    // Add mode — check duplicate
                    var existing = db.GetBudgetsByUser(_currentUser.UserID, _month)
                        .Find(b => b.CategoryId == categoryId);

                    if (existing != null)
                    {
                        MessageBox.Show(
                            "A budget for this category already exists for the selected month.\n" +
                            "Use the Edit button to update it.",
                            "Duplicate Budget",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    Budget newBudget = new Budget
                    {
                        UserId = _currentUser.UserID,
                        CategoryId = categoryId,
                        LimitAmount = limit,
                        Month = _month
                    };
                    db.InsertBudget(newBudget);
                }
                else
                {
                    // Edit mode
                    _existingBudget.LimitAmount = limit;
                    db.UpdateBudget(_existingBudget);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save budget: " + ex.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}