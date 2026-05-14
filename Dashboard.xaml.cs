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

			// Load avatar photo on dashboard if one is saved
			RefreshDashboardAvatar();
			RefreshDashboard();
		}

		// ── AVATAR ────────────────────────────────────────────────────────

		public void RefreshDashboardAvatar()
		{
			if (CurrentUser == null)
				return;

			var profile = db.GetProfileByUserId(CurrentUser.UserID);
			if (profile == null)
				return;

			if (string.IsNullOrWhiteSpace(profile.AvatarUrl))
				return;

			if (!System.IO.File.Exists(profile.AvatarUrl))
				return;

			try
			{
				var bmp = new BitmapImage();
				bmp.BeginInit();
				bmp.UriSource = new Uri(profile.AvatarUrl, UriKind.Absolute);
				bmp.CacheOption = BitmapCacheOption.OnLoad;
				bmp.EndInit();

				// Swap initial text for photo in the nav avatar
				NavAvatarImage.Source = bmp;
				NavAvatarImage.Visibility = Visibility.Visible;
				AvatarInitial.Visibility = Visibility.Collapsed;
			}
			catch
			{
				// If image fails to load just keep showing the initial
				NavAvatarImage.Visibility = Visibility.Collapsed;
				AvatarInitial.Visibility = Visibility.Visible;
			}
		}

		// ── DASHBOARD DATA ────────────────────────────────────────────────

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
				return 0;

			return (current - previous) / previous * 100;
		}

		private string GetArrow(decimal value)
		{
			return value > 0 ? "↗" : "↘";
		}

		private void SetKpiChangeStyle(TextBlock textBlock, decimal value)
		{
			if (value > 0)
				textBlock.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));
			else
				textBlock.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
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

			if (CurrentUser == null)
				return;

			try
			{
				string period = (PeriodComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "This Month";

				List<(int month, int year)> periods = new List<(int, int)>();
				int nowMonth = DateTime.Now.Month;
				int nowYear = DateTime.Now.Year;

				if (period == "Last 6 Months")
				{
					for (int i = 5; i >= 0; i--)
					{
						int m = nowMonth - i;
						int y = nowYear;
						while (m <= 0) { m += 12; y--; }
						periods.Add((m, y));
					}
				}
				else if (period == "Last Month")
				{
					int m = nowMonth == 1 ? 12 : nowMonth - 1;
					int y = nowMonth == 1 ? nowYear - 1 : nowYear;
					periods.Add((m, y));
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
			double w = 420, h = 190;
			double padL = 40, padB = 30, padT = 10;
			double chartW = w - padL - 10;
			double chartH = h - padB - padT;

			decimal maxVal = points.Max(p => Math.Max(p.Income, p.Expense));
			if (maxVal == 0) maxVal = 1;

			int n = points.Count;

			// Grid lines
			for (int i = 0; i <= 4; i++)
			{
				double y = padT + chartH - (chartH * i / 4);
				var line = new System.Windows.Shapes.Line
				{
					X1 = padL,
					Y1 = y,
					X2 = padL + chartW,
					Y2 = y,
					Stroke = gridBrush,
					StrokeThickness = 1
				};
				IncomeExpenseCanvas.Children.Add(line);

				decimal val = maxVal * i / 4;
				AddCanvasText(IncomeExpenseCanvas, $"€{val:N0}", 0, y - 8, mutedBrush, 10);
			}

			Func<int, double> getX = idx => padL + (n == 1 ? chartW / 2 : idx * chartW / (n - 1));
			Func<decimal, double> getY = val => padT + chartH - (double)(val / maxVal) * chartH;

			// Income line
			for (int i = 0; i < n - 1; i++)
			{
				IncomeExpenseCanvas.Children.Add(new System.Windows.Shapes.Line
				{
					X1 = getX(i),
					Y1 = getY(points[i].Income),
					X2 = getX(i + 1),
					Y2 = getY(points[i + 1].Income),
					Stroke = incomeBrush,
					StrokeThickness = 2
				});
			}

			// Expense line
			for (int i = 0; i < n - 1; i++)
			{
				IncomeExpenseCanvas.Children.Add(new System.Windows.Shapes.Line
				{
					X1 = getX(i),
					Y1 = getY(points[i].Expense),
					X2 = getX(i + 1),
					Y2 = getY(points[i + 1].Expense),
					Stroke = expenseBrush,
					StrokeThickness = 2
				});
			}

			// X labels
			for (int i = 0; i < n; i++)
			{
				AddCanvasText(IncomeExpenseCanvas, points[i].Label, getX(i) - 12, h - padB + 5, mutedBrush, 10);
			}
		}

		private void DrawPieChart()
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

		// ── ACTIVE NAV HIGHLIGHT ──────────────────────────────────────────
		// Resets every nav button to the muted slate color, then paints the
		// currently-active one in the brand green + bold. Called from every
		// NavX_Click so the highlight follows the page the user is on.
		private static readonly Brush NavMutedBrush = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
		private static readonly Brush NavActiveBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xB8, 0x94));

		private void HighlightNav(Button active)
		{
			Button[] all = { NavDashboardBtn, NavTransactionsBtn, NavCategoriesBtn,
							 NavAccountsBtn, NavBudgetBtn, NavGoalsBtn,
							 NavReportsBtn, NavGroupsBtn };
			foreach (var b in all)
			{
				b.Foreground = NavMutedBrush;
				b.FontWeight = FontWeights.Normal;
			}
			if (active != null)
			{
				active.Foreground = NavActiveBrush;
				active.FontWeight = FontWeights.Bold;
			}
		}

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
			MessageBox.Show("Budget page coming soon.", "Navigation");
		}

		private void NavGoals_Click(object sender, RoutedEventArgs e)
		{
			ShowDashboardView();
			HighlightNav(NavGoalsBtn);
			MessageBox.Show("Goals page coming soon.", "Navigation");
		}

		private ProfileView _profileView;
		private TransactionsView _transactionsView;
		private CategoriesView _categoriesView;
		private AccountsView _accountsView;
		private ReportsView _reportsView;

		private void NavProfile_Click(object sender, RoutedEventArgs e)
		{
			// Lazy-create the embedded view; rebuild each open so the form reflects current DB state
			ShowDashboardView();
			HighlightNav(null); // Profile isn't a top-nav item — clear all highlights
			_profileView = new ProfileView(CurrentUser);
			ProfileHost.Content = _profileView;

			DashboardContent.Visibility = Visibility.Collapsed;
			ProfileHost.Visibility = Visibility.Visible;
		}

		// Used by every "leave the current sub-page" path — tears down whichever embed is open
		private void ShowDashboardView()
		{
			ProfileHost.Visibility = Visibility.Collapsed;
			ProfileHost.Content = null;
			_profileView = null;

			TransactionsHost.Visibility = Visibility.Collapsed;
			TransactionsHost.Content = null;
			_transactionsView = null;

			CategoriesHost.Visibility = Visibility.Collapsed;
			CategoriesHost.Content = null;
			_categoriesView = null;

			AccountsHost.Visibility = Visibility.Collapsed;
			AccountsHost.Content = null;
			_accountsView = null;

			ReportsHost.Visibility = Visibility.Collapsed;
			ReportsHost.Content = null;
			_reportsView = null;

			DashboardContent.Visibility = Visibility.Visible;
			// Avatar may have changed while inside the profile
			RefreshDashboardAvatar();
		}

		private void NavGroups_Click(object sender, RoutedEventArgs e)
		{
			ShowDashboardView();
			HighlightNav(NavGroupsBtn);
			MessageBox.Show("Groups page coming soon.", "Navigation");
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

		private void ViewAllTransactions_Click(object sender, RoutedEventArgs e)
		{
			// Forward to the same embed flow as the top-nav Transactions button
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
