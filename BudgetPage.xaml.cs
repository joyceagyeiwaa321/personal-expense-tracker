using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FinancyApplication
{
    // ViewModel used for each budget row in the table
    public class BudgetRowViewModel
    {
        public int BudgetId { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal LimitAmount { get; set; }
        public decimal SpentAmount { get; set; }
        public decimal RemainingAmount => LimitAmount - SpentAmount;
        public double ProgressPercent => LimitAmount > 0
            ? Math.Min((double)(SpentAmount / LimitAmount) * 100, 100)
            : 0;
        public bool IsExceeded => SpentAmount > LimitAmount;
        public string Month { get; set; }
    }

    public partial class BudgetPage : Window
    {
        private readonly Data db = new Data();
        public User CurrentUser { get; set; }

        private bool isDarkMode = false;
        private List<BudgetRowViewModel> _allRows = new List<BudgetRowViewModel>();
        private Dictionary<int, string> _categoryNames = new Dictionary<int, string>();

        // Brushes matching Dashboard
        private readonly Brush gridBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235));
        private readonly Brush textBrush = new SolidColorBrush(Color.FromRgb(31, 41, 55));
        private readonly Brush mutedBrush = new SolidColorBrush(Color.FromRgb(100, 116, 139));
        private readonly Brush blueBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246));
        private readonly Brush redBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));

        private readonly Brush[] pieColors =
        {
            new SolidColorBrush(Color.FromRgb(34,  197, 94)),
            new SolidColorBrush(Color.FromRgb(59,  130, 246)),
            new SolidColorBrush(Color.FromRgb(245, 158, 11)),
            new SolidColorBrush(Color.FromRgb(239, 68,  68)),
            new SolidColorBrush(Color.FromRgb(168, 85,  247)),
            new SolidColorBrush(Color.FromRgb(20,  184, 166))
        };

        public BudgetPage()
        {
            InitializeComponent();
            Loaded += BudgetPage_Loaded;
        }

        // ── LOADED ────────────────────────────────────────────────────────

        private void BudgetPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current.Properties.Contains("CurrentUser"))
                CurrentUser = Application.Current.Properties["CurrentUser"] as User;

            if (CurrentUser != null && !string.IsNullOrWhiteSpace(CurrentUser.Username))
                AvatarInitial.Text = CurrentUser.Username.Substring(0, 1).ToUpper();

            PopulateMonthComboBox();
            LoadCategoryNames();
            PopulateCategoryFilter();
            RefreshPage();
        }

        // ── SETUP HELPERS ─────────────────────────────────────────────────

        private void PopulateMonthComboBox()
        {
            MonthComboBox.SelectionChanged -= MonthComboBox_SelectionChanged;
            MonthComboBox.Items.Clear();

            // Show last 12 months (most recent first)
            DateTime now = DateTime.Now;
            for (int i = 0; i < 12; i++)
            {
                DateTime d = now.AddMonths(-i);
                string value = d.ToString("yyyy-MM");
                string label = d.ToString("MMMM yyyy");
                MonthComboBox.Items.Add(new ComboBoxItem { Content = label, Tag = value });
            }

            MonthComboBox.SelectedIndex = 0;
            MonthComboBox.SelectionChanged += MonthComboBox_SelectionChanged;
        }

        private void LoadCategoryNames()
        {
            if (CurrentUser == null) return;
            try
            {
                _categoryNames = db.GetCategoriesByUser(CurrentUser.UserID)
                    .ToDictionary(c => c.CategoryID, c => c.Name);
            }
            catch { _categoryNames = new Dictionary<int, string>(); }
        }

        private void PopulateCategoryFilter()
        {
            CategoryFilterComboBox.SelectionChanged -= CategoryFilter_SelectionChanged;
            CategoryFilterComboBox.Items.Clear();
            CategoryFilterComboBox.Items.Add(new ComboBoxItem { Content = "All Categories", Tag = -1 });

            foreach (var kv in _categoryNames)
                CategoryFilterComboBox.Items.Add(new ComboBoxItem { Content = kv.Value, Tag = kv.Key });

            CategoryFilterComboBox.SelectedIndex = 0;
            CategoryFilterComboBox.SelectionChanged += CategoryFilter_SelectionChanged;
        }

        // ── REFRESH ───────────────────────────────────────────────────────

        private void RefreshPage()
        {
            LoadBudgetRows();
            ApplyCategoryFilter();
            DrawCharts();
        }

        private void LoadBudgetRows()
        {
            _allRows.Clear();

            if (CurrentUser == null) return;

            string month = GetSelectedMonth();

            try
            {
                List<Budget> budgets = db.GetBudgetsByUser(CurrentUser.UserID, month);

                foreach (Budget b in budgets)
                {
                    string catName = _categoryNames.ContainsKey(b.CategoryId)
                        ? _categoryNames[b.CategoryId]
                        : "Uncategorised";

                    decimal spent = db.GetSpentAmount(CurrentUser.UserID, b.CategoryId, month);

                    _allRows.Add(new BudgetRowViewModel
                    {
                        BudgetId = b.BudgetId,
                        CategoryId = b.CategoryId,
                        CategoryName = catName,
                        LimitAmount = b.LimitAmount,
                        SpentAmount = spent,
                        Month = month
                    });
                }
            }
            catch { /* show empty state */ }
        }

        private void ApplyCategoryFilter()
        {
            int selectedCatId = GetSelectedCategoryFilter();

            List<BudgetRowViewModel> filtered = selectedCatId == -1
                ? _allRows
                : _allRows.Where(r => r.CategoryId == selectedCatId).ToList();

            RenderBudgetRows(filtered);
            UpdateKpiCards(filtered);
        }

        // ── RENDER ROWS ───────────────────────────────────────────────────

        private void RenderBudgetRows(List<BudgetRowViewModel> rows)
        {
            BudgetRowsPanel.Children.Clear();

            if (rows.Count == 0)
            {
                NoBudgetsPanel.Visibility = Visibility.Visible;
                return;
            }

            NoBudgetsPanel.Visibility = Visibility.Collapsed;

            foreach (BudgetRowViewModel row in rows)
            {
                Grid rowGrid = new Grid { Margin = new Thickness(0, 6, 0, 6) };

                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

                // Category name
                TextBlock catText = new TextBlock
                {
                    Text = row.CategoryName,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 13,
                    Foreground = textBrush
                };
                Grid.SetColumn(catText, 0);
                rowGrid.Children.Add(catText);

                // Limit
                TextBlock limitText = new TextBlock
                {
                    Text = $"€ {row.LimitAmount:N0}",
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 13,
                    Foreground = textBrush
                };
                Grid.SetColumn(limitText, 1);
                rowGrid.Children.Add(limitText);

                // Spent
                TextBlock spentText = new TextBlock
                {
                    Text = $"€ {row.SpentAmount:N0}",
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 13,
                    Foreground = textBrush
                };
                Grid.SetColumn(spentText, 2);
                rowGrid.Children.Add(spentText);

                // Remaining
                Brush remainingColor = row.IsExceeded
                    ? new SolidColorBrush(Color.FromRgb(239, 68, 68))
                    : textBrush;

                TextBlock remainText = new TextBlock
                {
                    Text = row.IsExceeded
                        ? $"-€ {Math.Abs(row.RemainingAmount):N0}"
                        : $"€ {row.RemainingAmount:N0}",
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 13,
                    Foreground = remainingColor
                };
                Grid.SetColumn(remainText, 3);
                rowGrid.Children.Add(remainText);

                // Progress bar + label
                Grid progressContainer = new Grid { VerticalAlignment = VerticalAlignment.Center };
                progressContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                progressContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Track (background)
                Border track = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                    CornerRadius = new CornerRadius(4),
                    Height = 8,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                Grid.SetColumn(track, 0);

                // Fill (foreground) — we wrap in a Grid so fill sits on top of track
                Grid trackGrid = new Grid { Height = 8, Margin = new Thickness(0, 0, 8, 0) };
                Border trackBg = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                    CornerRadius = new CornerRadius(4)
                };

                double fillWidth = row.ProgressPercent / 100.0;
                Brush fillBrush = row.IsExceeded
                    ? new SolidColorBrush(Color.FromRgb(239, 68, 68))
                    : new SolidColorBrush(Color.FromRgb(0, 184, 148));

                Border trackFill = new Border
                {
                    Background = fillBrush,
                    CornerRadius = new CornerRadius(4),
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                // We use a SizeChanged trick to set relative width
                double capturedFill = fillWidth;
                trackFill.Loaded += (s, e) =>
                {
                    trackFill.Width = trackGrid.ActualWidth * capturedFill;
                };
                trackGrid.SizeChanged += (s, e) =>
                {
                    trackFill.Width = trackGrid.ActualWidth * capturedFill;
                };

                trackGrid.Children.Add(trackBg);
                trackGrid.Children.Add(trackFill);
                Grid.SetColumn(trackGrid, 0);

                TextBlock pctLabel = new TextBlock
                {
                    Text = $"{row.ProgressPercent:F0}% used",
                    FontSize = 11,
                    Foreground = mutedBrush,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(pctLabel, 1);

                progressContainer.Children.Add(trackGrid);
                progressContainer.Children.Add(pctLabel);

                Grid.SetColumn(progressContainer, 4);
                rowGrid.Children.Add(progressContainer);

                // Edit / Delete buttons
                StackPanel actionPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Button editBtn = new Button
                {
                    Content = "✎",
                    Style = (Style)FindResource("EditButton"),
                    Tag = row,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                editBtn.Click += EditBudget_Click;

                Button deleteBtn = new Button
                {
                    Content = "✕",
                    Style = (Style)FindResource("DeleteButton"),
                    Tag = row
                };
                deleteBtn.Click += DeleteBudget_Click;

                actionPanel.Children.Add(editBtn);
                actionPanel.Children.Add(deleteBtn);

                Grid.SetColumn(actionPanel, 5);
                rowGrid.Children.Add(actionPanel);

                BudgetRowsPanel.Children.Add(rowGrid);

                // Separator between rows
                BudgetRowsPanel.Children.Add(new Separator
                {
                    Background = new SolidColorBrush(Color.FromRgb(243, 244, 246)),
                    Margin = new Thickness(0, 2, 0, 2)
                });
            }
        }

        // ── KPI CARDS ─────────────────────────────────────────────────────

        private void UpdateKpiCards(List<BudgetRowViewModel> rows)
        {
            decimal totalBudget = rows.Sum(r => r.LimitAmount);
            decimal totalSpent = rows.Sum(r => r.SpentAmount);
            decimal remaining = totalBudget - totalSpent;

            string month = GetSelectedMonth();
            DateTime parsed = DateTime.ParseExact(month, "yyyy-MM", null);

            KpiTotalBudget.Text = $"€{totalBudget:N0}";
            KpiTotalBudgetSub.Text = $"for {parsed:MMMM yyyy}";

            KpiTotalSpent.Text = $"€{totalSpent:N0}";

            if (totalBudget > 0)
            {
                decimal pctSpent = totalSpent / totalBudget * 100;
                decimal prevMonth = GetPreviousMonthTotal(month);
                decimal change = prevMonth > 0
                    ? (totalSpent - prevMonth) / prevMonth * 100
                    : 0;

                string arrow = change >= 0 ? "↗" : "↘";
                KpiTotalSpentSub.Text = $"{Math.Abs(change):F0}% from last month {arrow}";
                KpiTotalSpentSub.Foreground = change <= 0
                    ? new SolidColorBrush(Color.FromRgb(34, 197, 94))
                    : new SolidColorBrush(Color.FromRgb(239, 68, 68));
            }
            else
            {
                KpiTotalSpentSub.Text = "No budget set";
                KpiTotalSpentSub.Foreground = mutedBrush;
            }

            KpiRemaining.Text = remaining >= 0
                ? $"€{remaining:N0}"
                : $"-€{Math.Abs(remaining):N0}";

            KpiRemaining.Foreground = remaining >= 0
                ? textBrush
                : new SolidColorBrush(Color.FromRgb(239, 68, 68));

            // Overall progress bar
            double overallPct = totalBudget > 0
                ? Math.Min((double)(totalSpent / totalBudget), 1.0)
                : 0;

            OverallProgressLabel.Text = $"{overallPct * 100:F0}% used";
            OverallProgressBar.Background = overallPct >= 1.0
                ? new SolidColorBrush(Color.FromRgb(239, 68, 68))
                : new SolidColorBrush(Color.FromRgb(0, 184, 148));

            // Set width after layout pass
            OverallProgressBar.Loaded += (s, e) => SetProgressBarWidth(overallPct);
            if (IsLoaded) SetProgressBarWidth(overallPct);
        }

        private void SetProgressBarWidth(double fraction)
        {
            double parentWidth = (OverallProgressBar.Parent as Grid)?.ActualWidth ?? 200;
            OverallProgressBar.Width = Math.Max(0, parentWidth * fraction);
        }

        private decimal GetPreviousMonthTotal(string month)
        {
            try
            {
                DateTime d = DateTime.ParseExact(month, "yyyy-MM", null).AddMonths(-1);
                string prevMonth = d.ToString("yyyy-MM");
                var budgets = db.GetBudgetsByUser(CurrentUser.UserID, prevMonth);
                return budgets.Sum(b => db.GetSpentAmount(CurrentUser.UserID, b.CategoryId, prevMonth));
            }
            catch { return 0; }
        }

        // ── CHARTS ────────────────────────────────────────────────────────

        private void DrawCharts()
        {
            DrawLineChart();
            DrawPieChart();
        }

        private void DrawLineChart()
        {
            BudgetLineCanvas.Children.Clear();

            if (CurrentUser == null)
            {
                AddCanvasText(BudgetLineCanvas, "No data yet", 20, 20, mutedBrush, 13);
                return;
            }

            // Gather last 6 months
            List<(string label, decimal budget, decimal spent)> points =
                new List<(string, decimal, decimal)>();

            for (int i = 5; i >= 0; i--)
            {
                DateTime d = DateTime.Now.AddMonths(-i);
                string m = d.ToString("yyyy-MM");
                try
                {
                    var budgets = db.GetBudgetsByUser(CurrentUser.UserID, m);
                    decimal bTotal = budgets.Sum(b => b.LimitAmount);
                    decimal sTotal = budgets.Sum(b => db.GetSpentAmount(CurrentUser.UserID, b.CategoryId, m));
                    points.Add((d.ToString("MMM"), bTotal, sTotal));
                }
                catch { points.Add((d.ToString("MMM"), 0, 0)); }
            }

            decimal maxVal = points.Max(p => Math.Max(p.budget, p.spent));
            if (maxVal <= 0)
            {
                AddCanvasText(BudgetLineCanvas, "No budget data yet", 20, 20, mutedBrush, 13);
                return;
            }

            double left = 55, top = 20, width = 315, height = 150;

            // Grid lines & y-axis labels
            for (int i = 0; i <= 4; i++)
            {
                double y = top + i * height / 4;
                BudgetLineCanvas.Children.Add(new Line
                {
                    X1 = left,
                    Y1 = y,
                    X2 = left + width,
                    Y2 = y,
                    Stroke = gridBrush,
                    StrokeThickness = 1
                });
                decimal label = maxVal - (maxVal / 4 * i);
                AddCanvasText(BudgetLineCanvas, $"{label:N0}", 2, y - 8, mutedBrush, 11);
            }

            // X-axis line
            BudgetLineCanvas.Children.Add(new Line
            {
                X1 = left,
                Y1 = top + height,
                X2 = left + width,
                Y2 = top + height,
                Stroke = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                StrokeThickness = 1
            });

            PointCollection budgetPts = new PointCollection();
            PointCollection spentPts = new PointCollection();

            for (int i = 0; i < points.Count; i++)
            {
                double x = left + i * (width / (points.Count - 1));
                double by = top + height - ((double)(points[i].budget / maxVal) * height);
                double sy = top + height - ((double)(points[i].spent / maxVal) * height);

                budgetPts.Add(new Point(x, by));
                spentPts.Add(new Point(x, sy));

                AddCanvasText(BudgetLineCanvas, points[i].label, x - 12, top + height + 8, mutedBrush, 11);
            }

            BudgetLineCanvas.Children.Add(new Polyline
            {
                Points = budgetPts,
                Stroke = blueBrush,
                StrokeThickness = 2.5
            });
            BudgetLineCanvas.Children.Add(new Polyline
            {
                Points = spentPts,
                Stroke = redBrush,
                StrokeThickness = 2.5
            });
        }

        private void DrawPieChart()
        {
            BudgetPieCanvas.Children.Clear();
            PieLegendPanel.Children.Clear();

            List<BudgetRowViewModel> rows = _allRows;
            if (rows.Count == 0)
            {
                AddCanvasText(BudgetPieCanvas, "No budgets set", 10, 80, mutedBrush, 13);
                return;
            }

            decimal total = rows.Sum(r => r.LimitAmount);
            if (total <= 0) return;

            double cx = 90, cy = 90, radius = 80;
            double startAngle = -90;

            for (int i = 0; i < rows.Count; i++)
            {
                double sliceAngle = (double)(rows[i].LimitAmount / total) * 360;
                double endAngle = startAngle + sliceAngle;

                Point startPt = GetCirclePoint(cx, cy, radius, startAngle);
                Point endPt = GetCirclePoint(cx, cy, radius, endAngle);

                PathFigure figure = new PathFigure { StartPoint = new Point(cx, cy) };
                figure.Segments.Add(new LineSegment(startPt, true));
                figure.Segments.Add(new ArcSegment(endPt, new Size(radius, radius), 0,
                    sliceAngle > 180, SweepDirection.Clockwise, true));
                figure.Segments.Add(new LineSegment(new Point(cx, cy), true));

                PathGeometry geo = new PathGeometry();
                geo.Figures.Add(figure);

                BudgetPieCanvas.Children.Add(new Path
                {
                    Fill = pieColors[i % pieColors.Length],
                    Data = geo
                });

                // Percentage label inside slice
                double midAngle = startAngle + sliceAngle / 2;
                Point labelPt = GetCirclePoint(cx, cy, radius * 0.62, midAngle);
                string pctStr = $"{(double)(rows[i].LimitAmount / total) * 100:F0}%";
                AddCanvasText(BudgetPieCanvas, pctStr, labelPt.X - 10, labelPt.Y - 7,
                    Brushes.White, 10);

                // Legend
                PieLegendPanel.Children.Add(new TextBlock
                {
                    Text = $"● {rows[i].CategoryName}",
                    Foreground = pieColors[i % pieColors.Length],
                    FontSize = 12,
                    Margin = new Thickness(0, 4, 0, 4)
                });

                startAngle = endAngle;
            }
        }

        // ── ADD / EDIT / DELETE ───────────────────────────────────────────

        private void AddBudget_Click(object sender, RoutedEventArgs e)
        {
            string month = GetSelectedMonth();
            var dialog = new BudgetDialog(CurrentUser, _categoryNames, null, month);
            if (dialog.ShowDialog() == true)
            {
                RefreshPage();
            }
        }

        private void EditBudget_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is BudgetRowViewModel row)
            {
                Budget existing = new Budget
                {
                    BudgetId = row.BudgetId,
                    UserId = CurrentUser.UserID,
                    CategoryId = row.CategoryId,
                    LimitAmount = row.LimitAmount,
                    Month = row.Month
                };

                var dialog = new BudgetDialog(CurrentUser, _categoryNames, existing, row.Month);
                if (dialog.ShowDialog() == true)
                    RefreshPage();
            }
        }

        private void DeleteBudget_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is BudgetRowViewModel row)
            {
                MessageBoxResult confirm = MessageBox.Show(
                    $"Delete the budget for '{row.CategoryName}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        db.DeleteBudget(row.BudgetId);
                        RefreshPage();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not delete budget: " + ex.Message);
                    }
                }
            }
        }

        // ── EVENT HANDLERS ────────────────────────────────────────────────

        private void MonthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) RefreshPage();
        }

        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) ApplyCategoryFilter();
        }

        // ── NAV (mirrors Dashboard) ───────────────────────────────────────

        private void NavDashboard_Click(object sender, RoutedEventArgs e)
        {
            Dashboard dash = new Dashboard { CurrentUser = CurrentUser };
            dash.Show();
            this.Close();
        }

        private void NavTransactions_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Transactions page is handled by Namariq.", "Navigation");
        }

        private void NavBudget_Click(object sender, RoutedEventArgs e) { /* already here */ }

        private void NavGoals_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Goals page coming soon.", "Navigation");
        }

        private void NavReports_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Reports page coming soon.", "Navigation");
        }

        private void NavGroups_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Groups page coming soon.", "Navigation");
        }

        private void NavProfile_Click(object sender, RoutedEventArgs e)
        {
            new ProfileWindow(CurrentUser).ShowDialog();
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

        // ── UTILS ─────────────────────────────────────────────────────────

        private string GetSelectedMonth()
        {
            return (MonthComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString()
                ?? DateTime.Now.ToString("yyyy-MM");
        }

        private int GetSelectedCategoryFilter()
        {
            object tag = (CategoryFilterComboBox.SelectedItem as ComboBoxItem)?.Tag;
            return tag != null ? (int)tag : -1;
        }

        private Point GetCirclePoint(double cx, double cy, double r, double angleDeg)
        {
            double rad = Math.PI * angleDeg / 180;
            return new Point(cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
        }

        private void AddCanvasText(Canvas canvas, string text, double left, double top,
            Brush color, double fontSize)
        {
            TextBlock tb = new TextBlock
            {
                Text = text,
                Foreground = color,
                FontSize = fontSize
            };
            canvas.Children.Add(tb);
            Canvas.SetLeft(tb, left);
            Canvas.SetTop(tb, top);
        }
    }
}