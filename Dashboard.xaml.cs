using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        private bool isDarkMode = false;

        private readonly Brush incomeBrush = new SolidColorBrush(Color.FromRgb(0, 184, 148));
        private readonly Brush expenseBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));
        private readonly Brush gridBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235));
        private readonly Brush textBrush = new SolidColorBrush(Color.FromRgb(31, 41, 55));
        private readonly Brush mutedBrush = new SolidColorBrush(Color.FromRgb(100, 116, 139));

        public Dashboard()
        {
            InitializeComponent();
            Loaded += Dashboard_Loaded;
        }

        private void Dashboard_Loaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current.Properties.Contains("CurrentUser"))
            {
                CurrentUser = Application.Current.Properties["CurrentUser"] as User;

                if (CurrentUser != null && !string.IsNullOrWhiteSpace(CurrentUser.Username))
                {
                    AvatarInitial.Text = CurrentUser.Username.Substring(0, 1).ToUpper();
                }
            }

            RefreshDashboard();
        }

        private void RefreshDashboard()
        {
            LoadKpis();
            LoadRecentTransactions();
            LoadCharts();
        }

        private void LoadKpis()
        {
            if (CurrentUser == null)
            {
                SetEmptyKpis();
                return;
            }

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
            catch
            {
                SetEmptyKpis();
            }
        }

        private void SetEmptyKpis()
        {
            KpiBalance.Text = "€0";
            KpiIncome.Text = "€0";
            KpiExpenses.Text = "€0";
            KpiSavings.Text = "0%";

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
                    ? (previousIncome - previousExpense) / previousIncome * 100
                    : 0;

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
            catch
            {
                SetEmptyKpis();
            }
        }

        private decimal GetPercentChange(decimal current, decimal previous)
        {
            if (previous == 0)
            {
                return 0;
            }

            return (current - previous) / previous * 100;
        }

        private string GetArrow(decimal value)
        {
            return value > 0 ? "↗" : "↘";
        }

        private void SetKpiChangeStyle(TextBlock textBlock, decimal value)
        {
            textBlock.Foreground = value > 0
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
                var txs = db.GetTransactionsByUser(
                        CurrentUser.UserID,
                        DateTime.Now.Month,
                        DateTime.Now.Year)
                    .Take(5)
                    .ToList();

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
            PieLegendPanel.Children.Clear();

            if (CurrentUser == null)
            {
                DrawEmptyMessage(IncomeExpenseCanvas, "No income or expense data yet", 20, 20);
                DrawEmptyMessage(ExpensePieCanvas, "No expense data yet", 0, 0);
                return;
            }

            try
            {
                DrawIncomeExpenseChart();
                DrawExpensePieChart();
            }
            catch
            {
                DrawEmptyMessage(IncomeExpenseCanvas, "No income or expense data yet", 20, 20);
                DrawEmptyMessage(ExpensePieCanvas, "No expense data yet", 0, 0);
            }
        }

        private void DrawIncomeExpenseChart()
        {
            IncomeExpenseCanvas.Children.Clear();

            List<MonthlyChartPoint> points = GetLastSixMonthData();

            decimal maxValue = points.Max(p => Math.Max(p.Income, p.Expense));

            if (maxValue <= 0)
            {
                DrawEmptyMessage(IncomeExpenseCanvas, "No income or expense data yet", 20, 20);
                return;
            }

            double left = 55;
            double top = 20;
            double width = 315;
            double height = 150;

            for (int i = 0; i <= 4; i++)
            {
                double y = top + i * height / 4;

                IncomeExpenseCanvas.Children.Add(new Line
                {
                    X1 = left,
                    Y1 = y,
                    X2 = left + width,
                    Y2 = y,
                    Stroke = gridBrush,
                    StrokeThickness = 1
                });

                decimal labelValue = maxValue - (maxValue / 4 * i);
                AddCanvasText(IncomeExpenseCanvas, $"{labelValue:N0}", 5, y - 8, mutedBrush, 11);
            }

            IncomeExpenseCanvas.Children.Add(new Line
            {
                X1 = left,
                Y1 = top + height,
                X2 = left + width,
                Y2 = top + height,
                Stroke = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                StrokeThickness = 1
            });

            PointCollection incomePoints = new PointCollection();
            PointCollection expensePoints = new PointCollection();

            for (int i = 0; i < points.Count; i++)
            {
                double x = left + i * (width / (points.Count - 1));

                double incomeY = top + height - ((double)(points[i].Income / maxValue) * height);
                double expenseY = top + height - ((double)(points[i].Expense / maxValue) * height);

                incomePoints.Add(new Point(x, incomeY));
                expensePoints.Add(new Point(x, expenseY));

                AddCanvasText(IncomeExpenseCanvas, points[i].Label, x - 12, top + height + 10, textBrush, 11);
            }

            IncomeExpenseCanvas.Children.Add(new Polyline
            {
                Points = incomePoints,
                Stroke = incomeBrush,
                StrokeThickness = 2.5
            });

            IncomeExpenseCanvas.Children.Add(new Polyline
            {
                Points = expensePoints,
                Stroke = expenseBrush,
                StrokeThickness = 2.5
            });
        }

        private List<MonthlyChartPoint> GetLastSixMonthData()
        {
            List<MonthlyChartPoint> result = new List<MonthlyChartPoint>();

            DateTime startMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-5);

            for (int i = 0; i < 6; i++)
            {
                DateTime date = startMonth.AddMonths(i);

                var txs = db.GetTransactionsByUser(CurrentUser.UserID, date.Month, date.Year).ToList();

                result.Add(new MonthlyChartPoint
                {
                    Label = date.ToString("MMM"),
                    Income = txs.Where(t => t.Type == "Income").Sum(t => t.Amount),
                    Expense = txs.Where(t => t.Type == "Expense").Sum(t => t.Amount)
                });
            }

            return result;
        }

        private void DrawExpensePieChart()
        {
            ExpensePieCanvas.Children.Clear();
            PieLegendPanel.Children.Clear();

            var txs = db.GetTransactionsByUser(
                    CurrentUser.UserID,
                    DateTime.Now.Month,
                    DateTime.Now.Year)
                .Where(t => t.Type == "Expense")
                .ToList();

            if (txs.Count == 0)
            {
                DrawEmptyMessage(ExpensePieCanvas, "No expense data yet", 0, 0);
                return;
            }

            Dictionary<int, string> categories = GetCategoryNames();

            var grouped = txs
                .GroupBy(t => t.CategoryID)
                .Select(g => new PieSliceData
                {
                    CategoryId = g.Key,
                    Category = categories.ContainsKey(g.Key) ? categories[g.Key] : "Uncategorised",
                    Total = g.Sum(t => t.Amount)
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            decimal grandTotal = grouped.Sum(g => g.Total);

            Brush[] colors =
            {
                new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                new SolidColorBrush(Color.FromRgb(139, 92, 246))
            };

            double centerX = 95;
            double centerY = 92;
            double radius = 78;
            double startAngle = -90;

            for (int i = 0; i < grouped.Count; i++)
            {
                double sliceAngle = (double)(grouped[i].Total / grandTotal) * 360;
                double endAngle = startAngle + sliceAngle;

                Point startPoint = GetCirclePoint(centerX, centerY, radius, startAngle);
                Point endPoint = GetCirclePoint(centerX, centerY, radius, endAngle);

                bool largeArc = sliceAngle > 180;

                PathFigure figure = new PathFigure
                {
                    StartPoint = new Point(centerX, centerY)
                };

                figure.Segments.Add(new LineSegment(startPoint, true));
                figure.Segments.Add(new ArcSegment(endPoint, new Size(radius, radius), 0, largeArc, SweepDirection.Clockwise, true));
                figure.Segments.Add(new LineSegment(new Point(centerX, centerY), true));

                PathGeometry geometry = new PathGeometry();
                geometry.Figures.Add(figure);

                ExpensePieCanvas.Children.Add(new Path
                {
                    Fill = colors[i % colors.Length],
                    Data = geometry
                });

                PieLegendPanel.Children.Add(new TextBlock
                {
                    Text = $"● {grouped[i].Category}",
                    Foreground = colors[i % colors.Length],
                    FontSize = 12,
                    Margin = new Thickness(0, 4, 0, 4)
                });

                startAngle = endAngle;
            }
        }

        private Dictionary<int, string> GetCategoryNames()
        {
            try
            {
                return db.GetCategoriesByUser(CurrentUser.UserID)
                    .ToDictionary(c => c.CategoryID, c => c.Name);
            }
            catch
            {
                return new Dictionary<int, string>();
            }
        }

        private Point GetCirclePoint(double centerX, double centerY, double radius, double angle)
        {
            double radians = Math.PI * angle / 180;

            return new Point(
                centerX + radius * Math.Cos(radians),
                centerY + radius * Math.Sin(radians)
            );
        }

        private void AddCanvasText(Canvas canvas, string text, double left, double top, Brush color, double fontSize)
        {
            TextBlock block = new TextBlock
            {
                Text = text,
                Foreground = color,
                FontSize = fontSize
            };

            canvas.Children.Add(block);
            Canvas.SetLeft(block, left);
            Canvas.SetTop(block, top);
        }

        private void DrawEmptyMessage(Canvas canvas, string message, double left, double top)
        {
            TextBlock block = new TextBlock
            {
                Text = message,
                Foreground = Brushes.Gray,
                FontSize = 13
            };

            canvas.Children.Add(block);
            Canvas.SetLeft(block, left);
            Canvas.SetTop(block, top);
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            isDarkMode = !isDarkMode;

            if (isDarkMode)
            {
                Background = new SolidColorBrush(Color.FromRgb(17, 24, 39));
                NavBar.Background = new SolidColorBrush(Color.FromRgb(31, 41, 55));
                LogoText.Foreground = Brushes.White;
                ThemeIcon.Text = "\uE708";
                ThemeIcon.Foreground = Brushes.White;
            }
            else
            {
                Background = new SolidColorBrush(Color.FromRgb(244, 246, 248));
                NavBar.Background = Brushes.White;
                LogoText.Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55));
                ThemeIcon.Text = "\uE706";
                ThemeIcon.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));
            }
        }

        private void PeriodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                LoadCharts();
            }
        }

        private void NavDashboard_Click(object sender, RoutedEventArgs e)
        {
            RefreshDashboard();
        }

        private void NavTransactions_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Transactions page is handled by Namariq.", "Navigation");
        }

        private void NavBudget_Click(object sender, RoutedEventArgs e)
        {
            new BudgetPage { CurrentUser = CurrentUser }.Show();
            this.Close();
        }

        private void NavGoals_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Goals page coming soon.", "Navigation");
        }

		private void NavProfile_Click(object sender, RoutedEventArgs e)
		{
			new ProfileWindow(CurrentUser).ShowDialog();
		}
		private void NavGroups_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Groups page coming soon.", "Navigation");
        }

        private void NavReports_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Reports page coming soon.", "Navigation");
        }

        private void ViewAllTransactions_Click(object sender, RoutedEventArgs e)
        {
            NavTransactions_Click(sender, e);
        }

        private class MonthlyChartPoint
        {
            public string Label { get; set; }
            public decimal Income { get; set; }
            public decimal Expense { get; set; }
        }

        private class PieSliceData
        {
            public int CategoryId { get; set; }
            public string Category { get; set; }
            public decimal Total { get; set; }
        }
    }
}