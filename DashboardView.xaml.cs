using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FinancyApplication
{
    public class TransactionViewModel
    {
        public string Description { get; set; }
        public string Category { get; set; }
        public string DateDisplay { get; set; }
        public string AmountDisplay { get; set; }
        public string AmountColor { get; set; }
    }

    public partial class DashboardView : UserControl
    {
        private readonly Data db = new Data();
        private readonly User _currentUser;

        public DashboardView(User user)
        {
            InitializeComponent();
            _currentUser = user;
            Loaded += (s, e) => LoadAll();
        }

        private void LoadAll()
        {
            MonthLabel.Text = DateTime.Now.ToString("MMMM yyyy") + " ▾";
            WelcomeText.Text = $"Welcome back, {_currentUser?.Username ?? "User"} 👋";
            WelcomeSubText.Text = $"Here's your financial summary for {DateTime.Now:MMMM yyyy}.";
            LoadKpis();
            LoadRecentTransactions();
        }

        private void LoadKpis()
        {
            if (_currentUser == null) return;
            try
            {
                int m = DateTime.Now.Month;
                int y = DateTime.Now.Year;

                var txs = db.GetTransactionsByUser(_currentUser.UserID, m, y);
                decimal inc = txs.Where(t => t.Type == "Income").Sum(t => t.Amount);
                decimal exp = txs.Where(t => t.Type == "Expense").Sum(t => t.Amount);
                decimal sav = inc > 0 ? (inc - exp) / inc * 100 : 0;

                int pm = m == 1 ? 12 : m - 1;
                int py = m == 1 ? y - 1 : y;
                var ptxs = db.GetTransactionsByUser(_currentUser.UserID, pm, py);
                decimal pi = ptxs.Where(t => t.Type == "Income").Sum(t => t.Amount);
                decimal pe = ptxs.Where(t => t.Type == "Expense").Sum(t => t.Amount);

                var accounts = db.GetAccountsByUser(_currentUser.UserID);
                decimal balance = accounts.Sum(a => a.Balance);

                KpiBalance.Text = $"€{balance:N2}";
                KpiBalanceSub.Text = accounts.Count == 1 ? "1 account" : $"{accounts.Count} accounts";
                KpiIncome.Text = $"€{inc:N2}";
                KpiExpenses.Text = $"€{exp:N2}";
                KpiSavings.Text = $"{sav:F1}%";

                if (pi > 0)
                {
                    decimal incChg = (inc - pi) / pi * 100;
                    KpiIncomeChange.Text = incChg >= 0
                        ? $"+{incChg:F0}% from last month ↗"
                        : $"{incChg:F0}% from last month ↘";
                    KpiIncomeChange.Foreground = incChg >= 0
                        ? new SolidColorBrush(Color.FromRgb(46, 204, 113))
                        : new SolidColorBrush(Color.FromRgb(255, 107, 107));
                }
                if (pe > 0)
                {
                    decimal expChg = (exp - pe) / pe * 100;
                    KpiExpensesChange.Text = expChg <= 0
                        ? $"{expChg:F0}% from last month ↘"
                        : $"+{expChg:F0}% from last month ↗";
                    KpiExpensesChange.Foreground = expChg <= 0
                        ? new SolidColorBrush(Color.FromRgb(46, 204, 113))
                        : new SolidColorBrush(Color.FromRgb(255, 107, 107));
                }
            }
            catch { }
        }

        private void LoadRecentTransactions()
        {
            if (_currentUser == null) { NoTxPanel.Visibility = Visibility.Visible; return; }
            try
            {
                var txs = db.GetTransactionsByUser(_currentUser.UserID,
                               DateTime.Now.Month, DateTime.Now.Year)
                            .Take(8).ToList();

                if (txs.Count == 0) { NoTxPanel.Visibility = Visibility.Visible; return; }

                var cats = db.GetCategoriesByUser(_currentUser.UserID)
                             .ToDictionary(c => c.CategoryID, c => c.Name);

                TxTable.ItemsSource = txs.Select(t => new TransactionViewModel
                {
                    Description = string.IsNullOrWhiteSpace(t.Description) ? "—" : t.Description,
                    Category = cats.ContainsKey(t.CategoryID) ? cats[t.CategoryID] : "Uncategorised",
                    DateDisplay = t.Date.ToString("dd MMM yyyy"),
                    AmountDisplay = t.Type == "Income" ? $"+ €{t.Amount:N0}" : $"- €{t.Amount:N0}",
                    AmountColor = t.Type == "Income" ? "#2ECC71" : "#FF6B6B"
                }).ToList();

                NoTxPanel.Visibility = Visibility.Collapsed;
            }
            catch { NoTxPanel.Visibility = Visibility.Visible; }
        }

        private void AddTransaction_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new TransactionDialog(_currentUser, null);
            dlg.Closed += didSave =>
            {
                ModalContent.Content = null;
                ModalHost.Visibility = Visibility.Collapsed;
                if (didSave) LoadAll();
            };
            ModalContent.Content = dlg;
            ModalHost.Visibility = Visibility.Visible;
        }

        private void ViewAllTransactions_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainAppWindow win)
                win.NavigateTo("Transactions");
        }
    }
}