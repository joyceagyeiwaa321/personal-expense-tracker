using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FinancyApplication
{
    public class TransactionViewModel
    {
        public string Description { get; set; }
        public string Category { get; set; }
        public string DateDisplay { get; set; }
        public string AmountDisplay { get; set; }
    }

    public partial class Dashboard : Window
    {
        private readonly Data db = new Data();

        public User CurrentUser { get; set; }

        private Brush incomeBrush => Dashboard_ThemeBrush("AccentBrush", Color.FromRgb(0, 184, 148));
        private Brush expenseBrush => Dashboard_ThemeBrush("DangerBrush", Color.FromRgb(239, 68, 68));
        private Brush gridBrush => Dashboard_ThemeBrush("BorderBrush", Color.FromRgb(229, 231, 235));
        private Brush textBrush => Dashboard_ThemeBrush("TextPrimaryBrush", Color.FromRgb(31, 41, 55));
        private Brush mutedBrush => Dashboard_ThemeBrush("TextSecondaryBrush", Color.FromRgb(100, 116, 139));

        private static Brush Dashboard_ThemeBrush(string key, Color fallback)
        {
            try
            {
                if (Application.Current != null && Application.Current.Resources.Contains(key))
                    return (Brush)Application.Current.Resources[key];
            }
            catch { }
            return new SolidColorBrush(fallback);
        }

        public Dashboard()
        {
            InitializeComponent();
            Loaded += Dashboard_Loaded;
            Closed += Dashboard_Closed;
            ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
        }

        private void Dashboard_Closed(object sender, EventArgs e)
        {
            ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
        }

        private void Dashboard_Loaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current.Properties.Contains("CurrentUser"))
            {
                CurrentUser = Application.Current.Properties["CurrentUser"] as User;
                if (CurrentUser != null && !string.IsNullOrWhiteSpace(CurrentUser.Username))
                    AvatarInitial.Text = CurrentUser.Username.Substring(0, 1).ToUpper();
            }

            UpdateThemeIcon();
            RefreshDashboardAvatar();
            RefreshDashboard();
            CheckGoalReminders();

            if (Application.Current.Properties.Contains("ShowPrivacyOnLoad") &&
                Application.Current.Properties["ShowPrivacyOnLoad"] is bool flag && flag)
            {
                Application.Current.Properties.Remove("ShowPrivacyOnLoad");
                FooterPrivacy_Click(this, new RoutedEventArgs());
            }
        }

        // AVATAR 

        public void RefreshDashboardAvatar()
        {
            if (CurrentUser == null) return;
            var profile = db.GetProfileByUserId(CurrentUser.UserID);
            if (profile == null || string.IsNullOrWhiteSpace(profile.AvatarUrl)) return;
            if (!System.IO.File.Exists(profile.AvatarUrl)) return;

            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(profile.AvatarUrl, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                NavAvatarImage.Source = bmp;
                NavAvatarImage.Visibility = Visibility.Visible;
                AvatarInitial.Visibility = Visibility.Collapsed;
            }
            catch
            {
                NavAvatarImage.Visibility = Visibility.Collapsed;
                AvatarInitial.Visibility = Visibility.Visible;
            }
        }

        // DASHBOARD DATA 

        private void RefreshDashboard()
        {
            LoadKpis();
            LoadRecentTransactions();
            LoadCharts();
        }

        private void LoadKpis()
        {
            if (CurrentUser == null) { SetEmptyKpis(); return; }
            try
            {
                int month = DateTime.Now.Month;
                int year = DateTime.Now.Year;
                var txs = db.GetTransactionsByUser(CurrentUser.UserID, month, year).ToList();
                decimal income = txs.Where(t => t.Type == "Income").Sum(t => t.Amount);
                decimal expense = txs.Where(t => t.Type == "Expense").Sum(t => t.Amount);
                decimal savingsRate = income > 0 ? (income - expense) / income * 100 : 0;
                var accounts = db.GetAccountsByUser(CurrentUser.UserID);
                decimal balance = accounts.Sum(a => a.Balance);
                KpiBalance.Text = $"€{balance:N0}";
                KpiIncome.Text = $"€{income:N0}";
                KpiExpenses.Text = $"€{expense:N0}";
                KpiSavings.Text = $"{savingsRate:F0}%";
                LoadKpiChanges(income, expense, savingsRate);
            }
            catch { SetEmptyKpis(); }
        }

        private void SetEmptyKpis()
        {
            KpiBalance.Text = "€0"; KpiIncome.Text = "€0";
            KpiExpenses.Text = "€0"; KpiSavings.Text = "0%";
            KpiBalanceSub.Text = "€0 this month  ↘";
            KpiIncomeChange.Text = "0% from last month  ↘";
            KpiExpensesChange.Text = "0% from last month  ↘";
            KpiSavingsChange.Text = "0% improvement  ↘";
            SetKpiChangeStyle(KpiBalanceSub, 0);
            SetKpiChangeStyle(KpiIncomeChange, 0);
            SetKpiChangeStyle(KpiExpensesChange, 0);
            SetKpiChangeStyle(KpiSavingsChange, 0);
        }

        private void LoadKpiChanges(decimal income, decimal expense, decimal savingsRate)
        {
            int currentMonth = DateTime.Now.Month;
            int currentYear = DateTime.Now.Year;
            int previousMonth = currentMonth == 1 ? 12 : currentMonth - 1;
            int previousYear = currentMonth == 1 ? currentYear - 1 : currentYear;
            try
            {
                var previousTxs = db.GetTransactionsByUser(CurrentUser.UserID, previousMonth, previousYear).ToList();
                decimal previousIncome = previousTxs.Where(t => t.Type == "Income").Sum(t => t.Amount);
                decimal previousExpense = previousTxs.Where(t => t.Type == "Expense").Sum(t => t.Amount);
                decimal previousSavings = previousIncome > 0
                    ? (previousIncome - previousExpense) / previousIncome * 100 : 0;
                decimal netThisMonth = income - expense;
                decimal incomeChange = previousIncome > 0 ? GetPercentChange(income, previousIncome) : 0;
                decimal expenseChange = previousExpense > 0 ? GetPercentChange(expense, previousExpense) : 0;
                decimal savingsChange = savingsRate - previousSavings;
                KpiBalanceSub.Text = $"€{netThisMonth:N0} this month {GetArrow(netThisMonth)}";
                KpiIncomeChange.Text = $"{incomeChange:F0}% from last month {GetArrow(incomeChange)}";
                KpiExpensesChange.Text = $"{expenseChange:F0}% from last month {GetArrow(-expenseChange)}";
                KpiSavingsChange.Text = $"{savingsChange:F0}% improvement {GetArrow(savingsChange)}";
                SetKpiChangeStyle(KpiBalanceSub, netThisMonth);
                SetKpiChangeStyle(KpiIncomeChange, incomeChange);
                SetKpiChangeStyle(KpiExpensesChange, -expenseChange);
                SetKpiChangeStyle(KpiSavingsChange, savingsChange);
            }
            catch { SetEmptyKpis(); }
        }

        private decimal GetPercentChange(decimal current, decimal previous)
            => previous == 0 ? 0 : (current - previous) / previous * 100;

        private string GetArrow(decimal value) => value > 0 ? "↗" : "↘";

        private void SetKpiChangeStyle(TextBlock tb, decimal value)
        {
            tb.Foreground = value > 0
                ? new SolidColorBrush(Color.FromRgb(34, 197, 94))
                : new SolidColorBrush(Color.FromRgb(239, 68, 68));
        }

        private void LoadRecentTransactions()
        {
            if (CurrentUser == null)
            {
                NoTxPanel.Visibility = Visibility.Visible;
                TxTable.ItemsSource = null;
                return;
            }
            try
            {
                var txs = db.GetTransactionsByUser(CurrentUser.UserID, DateTime.Now.Month, DateTime.Now.Year)
                            .Take(5).ToList();
                if (txs.Count == 0)
                {
                    NoTxPanel.Visibility = Visibility.Visible;
                    TxTable.ItemsSource = null;
                    return;
                }
                Dictionary<int, string> categories = GetCategoryNames();
                TxTable.ItemsSource = txs.Select(t => new TransactionViewModel
                {
                    Description = string.IsNullOrWhiteSpace(t.Description) ? "Transaction" : t.Description,
                    Category = categories.ContainsKey(t.CategoryID) ? categories[t.CategoryID] : "Uncategorised",
                    DateDisplay = t.Date.ToString("dd MMM yyyy"),
                    AmountDisplay = t.Type == "Income" ? $"+ €{t.Amount:N0}" : $"- €{t.Amount:N0}"
                }).ToList();
                NoTxPanel.Visibility = Visibility.Collapsed;
            }
            catch
            {
                NoTxPanel.Visibility = Visibility.Visible;
                TxTable.ItemsSource = null;
            }
        }

        private void LoadCharts()
        {
            IncomeExpenseCanvas.Children.Clear();
            ExpensePieCanvas.Children.Clear();
            if (CurrentUser == null) return;
            try
            {
                string period = (PeriodComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "This Month";
                var periods = new List<(int month, int year)>();
                int nowMonth = DateTime.Now.Month, nowYear = DateTime.Now.Year;
                if (period == "Last 6 Months")
                {
                    for (int i = 5; i >= 0; i--)
                    {
                        int m = nowMonth - i, y = nowYear;
                        while (m <= 0) { m += 12; y--; }
                        periods.Add((m, y));
                    }
                }
                else if (period == "Last Month")
                {
                    periods.Add((nowMonth == 1 ? 12 : nowMonth - 1, nowMonth == 1 ? nowYear - 1 : nowYear));
                }
                else
                {
                    periods.Add((nowMonth, nowYear));
                }
                var chartPoints = new List<MonthlyChartPoint>();
                foreach (var (m, y) in periods)
                {
                    var txs = db.GetTransactionsByUser(CurrentUser.UserID, m, y).ToList();
                    chartPoints.Add(new MonthlyChartPoint
                    {
                        Label = new DateTime(y, m, 1).ToString("MMM"),
                        Income = txs.Where(t => t.Type == "Income").Sum(t => t.Amount),
                        Expense = txs.Where(t => t.Type == "Expense").Sum(t => t.Amount)
                    });
                }
                DrawLineChart(chartPoints);
                DrawPieChart();
            }
            catch { }
        }

        private void DrawLineChart(List<MonthlyChartPoint> points)
        {
            double w = 420, h = 190, padL = 40, padB = 30, padT = 10;
            double chartW = w - padL - 10, chartH = h - padB - padT;
            decimal maxVal = points.Max(p => Math.Max(p.Income, p.Expense));
            if (maxVal == 0) maxVal = 1;
            int n = points.Count;
            for (int i = 0; i <= 4; i++)
            {
                double y = padT + chartH - (chartH * i / 4);
                IncomeExpenseCanvas.Children.Add(new Line { X1 = padL, Y1 = y, X2 = padL + chartW, Y2 = y, Stroke = gridBrush, StrokeThickness = 1 });
                AddCanvasText(IncomeExpenseCanvas, $"€{maxVal * i / 4:N0}", 0, y - 8, mutedBrush, 10);
            }
            Func<int, double> getX = idx => padL + (n == 1 ? chartW / 2 : idx * chartW / (n - 1));
            Func<decimal, double> getY = val => padT + chartH - (double)(val / maxVal) * chartH;
            for (int i = 0; i < n - 1; i++)
            {
                IncomeExpenseCanvas.Children.Add(new Line { X1 = getX(i), Y1 = getY(points[i].Income), X2 = getX(i + 1), Y2 = getY(points[i + 1].Income), Stroke = incomeBrush, StrokeThickness = 2 });
                IncomeExpenseCanvas.Children.Add(new Line { X1 = getX(i), Y1 = getY(points[i].Expense), X2 = getX(i + 1), Y2 = getY(points[i + 1].Expense), Stroke = expenseBrush, StrokeThickness = 2 });
            }
            for (int i = 0; i < n; i++)
            {
                var incDot = new Ellipse { Width = 7, Height = 7, Fill = incomeBrush };
                IncomeExpenseCanvas.Children.Add(incDot);
                Canvas.SetLeft(incDot, getX(i) - 3.5);
                Canvas.SetTop(incDot, getY(points[i].Income) - 3.5);

                var expDot = new Ellipse { Width = 7, Height = 7, Fill = expenseBrush };
                IncomeExpenseCanvas.Children.Add(expDot);
                Canvas.SetLeft(expDot, getX(i) - 3.5);
                Canvas.SetTop(expDot, getY(points[i].Expense) - 3.5);
            }
            for (int i = 0; i < n; i++)
                AddCanvasText(IncomeExpenseCanvas, points[i].Label, getX(i) - 12, h - padB + 5, mutedBrush, 10);
        }

        private void DrawPieChart()
        {
            ExpensePieCanvas.Children.Clear();
            PieLegendPanel.Children.Clear();
            var txs = db.GetTransactionsByUser(CurrentUser.UserID, DateTime.Now.Month, DateTime.Now.Year)
                        .Where(t => t.Type == "Expense").ToList();
            if (txs.Count == 0) { DrawEmptyMessage(ExpensePieCanvas, "No expense data yet", 0, 0); return; }
            Dictionary<int, string> categories = GetCategoryNames();
            var grouped = txs.GroupBy(t => t.CategoryID)
                .Select(g => new PieSliceData { CategoryId = g.Key, Category = categories.ContainsKey(g.Key) ? categories[g.Key] : "Uncategorised", Total = g.Sum(t => t.Amount) })
                .OrderByDescending(x => x.Total).ToList();
            decimal grandTotal = grouped.Sum(g => g.Total);
            Brush[] colors = {
                new SolidColorBrush(Color.FromRgb(34,197,94)),  new SolidColorBrush(Color.FromRgb(59,130,246)),
                new SolidColorBrush(Color.FromRgb(245,158,11)), new SolidColorBrush(Color.FromRgb(239,68,68)),
                new SolidColorBrush(Color.FromRgb(100,116,139)),new SolidColorBrush(Color.FromRgb(139,92,246))
            };
            double cx = 95, cy = 92, radius = 78, startAngle = -90;
            for (int i = 0; i < grouped.Count; i++)
            {
                double sliceAngle = (double)(grouped[i].Total / grandTotal) * 360;
                double endAngle = startAngle + sliceAngle;
                Point startPt = GetCirclePoint(cx, cy, radius, startAngle);
                Point endPt = GetCirclePoint(cx, cy, radius, endAngle);
                PathFigure figure = new PathFigure { StartPoint = new Point(cx, cy) };
                figure.Segments.Add(new LineSegment(startPt, true));
                figure.Segments.Add(new ArcSegment(endPt, new Size(radius, radius), 0, sliceAngle > 180, SweepDirection.Clockwise, true));
                figure.Segments.Add(new LineSegment(new Point(cx, cy), true));
                PathGeometry geo = new PathGeometry();
                geo.Figures.Add(figure);
                ExpensePieCanvas.Children.Add(new Path { Fill = colors[i % colors.Length], Data = geo });
                PieLegendPanel.Children.Add(new TextBlock { Text = $"● {grouped[i].Category}", Foreground = colors[i % colors.Length], FontSize = 12, Margin = new Thickness(0, 4, 0, 4) });
                startAngle = endAngle;
            }
        }

        private Dictionary<int, string> GetCategoryNames()
        {
            try { return db.GetCategoriesByUser(CurrentUser.UserID).ToDictionary(c => c.CategoryID, c => c.Name); }
            catch { return new Dictionary<int, string>(); }
        }

        private Point GetCirclePoint(double cx, double cy, double r, double angle)
        {
            double rad = Math.PI * angle / 180;
            return new Point(cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
        }

        private void AddCanvasText(Canvas canvas, string text, double left, double top, Brush color, double fontSize)
        {
            var tb = new TextBlock { Text = text, Foreground = color, FontSize = fontSize };
            canvas.Children.Add(tb);
            Canvas.SetLeft(tb, left); Canvas.SetTop(tb, top);
        }

        private void DrawEmptyMessage(Canvas canvas, string message, double left, double top)
        {
            var tb = new TextBlock { Text = message, Foreground = Brushes.Gray, FontSize = 13 };
            canvas.Children.Add(tb);
            Canvas.SetLeft(tb, left); Canvas.SetTop(tb, top);
        }

        // THEME 

        private void ThemeToggle_Click(object sender, RoutedEventArgs e) => ThemeManager.Toggle();

        private void UpdateThemeIcon()
        {
            if (ThemeIcon == null) return;
            ThemeIcon.Text = ThemeManager.IsDarkMode ? "\uE708" : "\uE706";
        }

        private void ThemeManager_ThemeChanged(object sender, EventArgs e)
        {
            UpdateThemeIcon();
            try { LoadCharts(); } catch { }
        }

        private void PeriodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) LoadCharts();
        }

        // NAV HIGHLIGHT 

        private static readonly Brush NavMutedBrush = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
        private static readonly Brush NavActiveBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xB8, 0x94));

        private void HighlightNav(Button active)
        {
            Button[] all = { NavDashboardBtn, NavTransactionsBtn, NavCategoriesBtn,
                             NavAccountsBtn, NavBudgetBtn, NavGoalsBtn,
                             NavReportsBtn, NavGroupsBtn };
            foreach (var b in all) { b.Foreground = NavMutedBrush; b.FontWeight = FontWeights.Normal; }
            if (active != null) { active.Foreground = NavActiveBrush; active.FontWeight = FontWeights.Bold; }
        }

        // NAV CLICKS 

        private void NavDashboard_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboardView();
            HighlightNav(NavDashboardBtn);
            RefreshDashboard();
        }

        private void NavTransactions_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboardView();
            HighlightNav(NavTransactionsBtn);
            _transactionsView = new TransactionsView(CurrentUser);
            TransactionsHost.Content = _transactionsView;
            DashboardContent.Visibility = Visibility.Collapsed;
            TransactionsHost.Visibility = Visibility.Visible;
        }

        private void NavCategories_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboardView();
            HighlightNav(NavCategoriesBtn);
            _categoriesView = new CategoriesView(CurrentUser);
            CategoriesHost.Content = _categoriesView;
            DashboardContent.Visibility = Visibility.Collapsed;
            CategoriesHost.Visibility = Visibility.Visible;
        }

        private void NavAccounts_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboardView();
            HighlightNav(NavAccountsBtn);
            _accountsView = new AccountsView(CurrentUser);
            AccountsHost.Content = _accountsView;
            DashboardContent.Visibility = Visibility.Collapsed;
            AccountsHost.Visibility = Visibility.Visible;
        }

        private void NavBudget_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboardView();
            HighlightNav(NavBudgetBtn);
            _budgetView = new BudgetPage(CurrentUser);  
            BudgetHost.Content = _budgetView;
            DashboardContent.Visibility = Visibility.Collapsed;
            BudgetHost.Visibility = Visibility.Visible;
        }

        private void NavGoals_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboardView();
            HighlightNav(NavGoalsBtn);
            _goalsView = new GoalsView(CurrentUser);
            GoalsHost.Content = _goalsView;
            DashboardContent.Visibility = Visibility.Collapsed;
            GoalsHost.Visibility = Visibility.Visible;
        }

        private void NavReports_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboardView();
            HighlightNav(NavReportsBtn);
            _reportsView = new ReportsView(CurrentUser);
            ReportsHost.Content = _reportsView;
            DashboardContent.Visibility = Visibility.Collapsed;
            ReportsHost.Visibility = Visibility.Visible;
        }

        private void NavGroups_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboardView();
            HighlightNav(NavGroupsBtn);
            _groupsView = new GroupsView(CurrentUser);  // ✅ fixed
            GroupsHost.Content = _groupsView;
            DashboardContent.Visibility = Visibility.Collapsed;
            GroupsHost.Visibility = Visibility.Visible;
        }

        private void NavProfile_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboardView();
            HighlightNav(null);
            _profileView = new ProfileView(CurrentUser);
            ProfileHost.Content = _profileView;
            DashboardContent.Visibility = Visibility.Collapsed;
            ProfileHost.Visibility = Visibility.Visible;
        }

        // FIELDS 

        private ProfileView _profileView;
        private TransactionsView _transactionsView;
        private CategoriesView _categoriesView;
        private AccountsView _accountsView;
        private ReportsView _reportsView;
        private PrivacyView _privacyView;
        private BudgetPage _budgetView; 
        private GroupsView _groupsView;
        private GoalsView _goalsView;

        // SHOW DASHBOARD (teardown all hosts) 

        private void ShowDashboardView()
        {
            ProfileHost.Visibility = Visibility.Collapsed; ProfileHost.Content = null; _profileView = null;
            TransactionsHost.Visibility = Visibility.Collapsed; TransactionsHost.Content = null; _transactionsView = null;
            CategoriesHost.Visibility = Visibility.Collapsed; CategoriesHost.Content = null; _categoriesView = null;
            AccountsHost.Visibility = Visibility.Collapsed; AccountsHost.Content = null; _accountsView = null;
            ReportsHost.Visibility = Visibility.Collapsed; ReportsHost.Content = null; _reportsView = null;
            BudgetHost.Visibility = Visibility.Collapsed; BudgetHost.Content = null; _budgetView = null;
            GroupsHost.Visibility = Visibility.Collapsed; GroupsHost.Content = null; _groupsView = null;
            GoalsHost.Visibility = Visibility.Collapsed; GoalsHost.Content = null; _goalsView = null; 

            if (_privacyView != null) { _privacyView.BackRequested -= PrivacyView_BackRequested; _privacyView = null; }
            PrivacyHost.Visibility = Visibility.Collapsed; PrivacyHost.Content = null;

            DashboardContent.Visibility = Visibility.Visible;
            RefreshDashboardAvatar();
        }

        // FOOTER / PRIVACY

        private void FooterPrivacy_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboardView();
            HighlightNav(null);
            _privacyView = new PrivacyView();
            _privacyView.BackRequested += PrivacyView_BackRequested;
            PrivacyHost.Content = _privacyView;
            DashboardContent.Visibility = Visibility.Collapsed;
            PrivacyHost.Visibility = Visibility.Visible;
        }

        private void PrivacyView_BackRequested(object sender, EventArgs e)
        {
            ShowDashboardView();
            HighlightNav(NavDashboardBtn);
            RefreshDashboard();
        }

        private void ViewAllTransactions_Click(object sender, RoutedEventArgs e)
            => NavTransactions_Click(sender, e);

        // INNER CLASSES 

        private class MonthlyChartPoint { public string Label { get; set; } public decimal Income { get; set; } public decimal Expense { get; set; } }
        private class PieSliceData { public int CategoryId { get; set; } public string Category { get; set; } public decimal Total { get; set; } }

        private void CheckGoalReminders()
        {
            Task.Run(() =>
            {
                try
                {
                    if (CurrentUser == null) return;
                    var profile = db.GetProfileByUserId(CurrentUser.UserID);
                    if (profile == null || !profile.NotifGoalReminders) return;
                    if (string.IsNullOrEmpty(CurrentUser.Email)) return;

                    var goals = db.GetGoalsByUser(CurrentUser.UserID);
                    var emailService = new EmailService();

                    foreach (var goal in goals)
                    {
                        if (goal.IsCompleted()) continue;
                        emailService.SendGoalReminder(
                            CurrentUser.Email,
                            CurrentUser.Username,
                            goal.Name,
                            goal.TargetAmount,
                            goal.SavedAmount
                        );
                    }
                }
                catch { }
            });
        }
    }
}