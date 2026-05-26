using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FinancyApplication
{
    public partial class ReportsView : UserControl
    {
        private readonly Data db = new Data();
        private readonly User _user;

        private readonly Brush incomeBrush = new SolidColorBrush(Color.FromRgb(0, 184, 148));
        private readonly Brush expenseBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));
        private readonly Brush gridBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235));
        private readonly Brush mutedBrush = new SolidColorBrush(Color.FromRgb(100, 116, 139));
        private readonly Brush textBrush = new SolidColorBrush(Color.FromRgb(31, 41, 55));

        private readonly Brush[] paletteBrushes =
        {
            new SolidColorBrush(Color.FromRgb(0,   184, 148)),
            new SolidColorBrush(Color.FromRgb(59,  130, 246)),
            new SolidColorBrush(Color.FromRgb(245, 158, 11)),
            new SolidColorBrush(Color.FromRgb(239, 68,  68)),
            new SolidColorBrush(Color.FromRgb(139, 92,  246)),
            new SolidColorBrush(Color.FromRgb(100, 116, 139))
        };

        public ReportsView(User user)
        {
            InitializeComponent();
            _user = user;
            Loaded += ReportsView_Loaded;
        }

        private void ReportsView_Loaded(object sender, RoutedEventArgs e)
        {
            PopulatePeriodPickers();
            RefreshAll();
        }

        private void PopulatePeriodPickers()
        {
            MonthCombo.Items.Clear();
            for (int m = 1; m <= 12; m++)
                MonthCombo.Items.Add(new ComboBoxItem
                {
                    Content = new DateTime(2000, m, 1).ToString("MMMM"),
                    Tag = m
                });

            YearCombo.Items.Clear();
            int thisYear = DateTime.Now.Year;
            for (int y = thisYear - 4; y <= thisYear + 1; y++)
                YearCombo.Items.Add(new ComboBoxItem { Content = y.ToString(), Tag = y });

            MonthCombo.SelectedIndex = DateTime.Now.Month - 1;
            YearCombo.SelectedItem = YearCombo.Items
                .Cast<ComboBoxItem>()
                .FirstOrDefault(i => (int)i.Tag == thisYear);
        }

        private (int month, int year) GetSelectedPeriod()
        {
            int m = MonthCombo.SelectedItem is ComboBoxItem mi ? (int)mi.Tag : DateTime.Now.Month;
            int y = YearCombo.SelectedItem is ComboBoxItem yi ? (int)yi.Tag : DateTime.Now.Year;
            return (m, y);
        }

        private void Period_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (_user == null) return;
            var (m, y) = GetSelectedPeriod();
            var txs = db.GetTransactionsByUser(_user.UserID, m, y) ?? new List<Transaction>();
            var categories = GetCategoryNames();
            LoadKpis(txs);
            DrawTrend();
            DrawBreakdown(txs, categories, m, y);
            LoadTopCategories(txs, categories);
            LoadTopTransactions(txs, categories);
        }

        private Dictionary<int, string> GetCategoryNames()
        {
            var dict = new Dictionary<int, string>();
            try { foreach (var c in db.GetCategoriesByUser(_user.UserID)) if (!dict.ContainsKey(c.CategoryID)) dict[c.CategoryID] = c.Name; }
            catch { }
            return dict;
        }

        private void LoadKpis(List<Transaction> txs)
        {
            decimal income = txs.Where(t => t.Type == "Income").Sum(t => t.Amount);
            decimal expense = txs.Where(t => t.Type == "Expense").Sum(t => t.Amount);
            decimal net = income - expense;
            decimal rate = income == 0 ? 0 : (net / income) * 100m;
            KpiIncome.Text = $"€{income:N2}";
            KpiExpense.Text = $"€{expense:N2}";
            KpiNet.Text = $"€{net:N2}";
            KpiNet.Foreground = net < 0 ? new SolidColorBrush(Color.FromRgb(239, 68, 68)) : textBrush;
            KpiRate.Text = income == 0 ? "—" : $"{rate:N1}%";
        }

        private void DrawTrend()
        {
            TrendCanvas.Children.Clear();
            var (curM, curY) = GetSelectedPeriod();
            var points = new List<(string label, decimal income, decimal expense)>();
            for (int i = 5; i >= 0; i--)
            {
                int m = curM - i, y = curY;
                while (m <= 0) { m += 12; y--; }
                var txs = db.GetTransactionsByUser(_user.UserID, m, y) ?? new List<Transaction>();
                points.Add((new DateTime(y, m, 1).ToString("MMM"),
                    txs.Where(t => t.Type == "Income").Sum(t => t.Amount),
                    txs.Where(t => t.Type == "Expense").Sum(t => t.Amount)));
            }
            double w = 900, h = 220, padL = 50, padR = 20, padT = 12, padB = 30;
            double chartW = w - padL - padR, chartH = h - padT - padB;
            decimal maxVal = points.Max(p => Math.Max(p.income, p.expense));
            if (maxVal == 0) maxVal = 1;
            for (int i = 0; i <= 4; i++)
            {
                double yy = padT + chartH - (chartH * i / 4);
                TrendCanvas.Children.Add(new Line { X1 = padL, Y1 = yy, X2 = padL + chartW, Y2 = yy, Stroke = gridBrush, StrokeThickness = 1 });
                AddCanvasText(TrendCanvas, $"€{maxVal * i / 4:N0}", 4, yy - 8, mutedBrush, 10);
            }
            int n = points.Count;
            Func<int, double> getX = idx => padL + (n == 1 ? chartW / 2 : idx * chartW / (n - 1));
            Func<decimal, double> getY = val => padT + chartH - (double)(val / maxVal) * chartH;
            for (int i = 0; i < n - 1; i++)
            {
                TrendCanvas.Children.Add(new Line { X1 = getX(i), Y1 = getY(points[i].income), X2 = getX(i + 1), Y2 = getY(points[i + 1].income), Stroke = incomeBrush, StrokeThickness = 2.5 });
                TrendCanvas.Children.Add(new Line { X1 = getX(i), Y1 = getY(points[i].expense), X2 = getX(i + 1), Y2 = getY(points[i + 1].expense), Stroke = expenseBrush, StrokeThickness = 2.5 });
            }
            for (int i = 0; i < n; i++)
            {
                TrendCanvas.Children.Add(new Ellipse { Width = 6, Height = 6, Fill = incomeBrush, Margin = new Thickness(getX(i) - 3, getY(points[i].income) - 3, 0, 0) });
                TrendCanvas.Children.Add(new Ellipse { Width = 6, Height = 6, Fill = expenseBrush, Margin = new Thickness(getX(i) - 3, getY(points[i].expense) - 3, 0, 0) });
                AddCanvasText(TrendCanvas, points[i].label, getX(i) - 12, padT + chartH + 6, mutedBrush, 11);
            }
        }

        private void DrawBreakdown(List<Transaction> txs, Dictionary<int, string> categories, int month, int year)
        {
            PieCanvas.Children.Clear();
            PieLegend.Children.Clear();
            BreakdownSubtitle.Text = $"Expenses for {new DateTime(year, month, 1):MMMM yyyy}";
            var expenses = txs.Where(t => t.Type == "Expense").ToList();
            if (expenses.Count == 0) { PieEmpty.Visibility = Visibility.Visible; PieCanvas.Visibility = Visibility.Collapsed; return; }
            PieEmpty.Visibility = Visibility.Collapsed; PieCanvas.Visibility = Visibility.Visible;
            var grouped = expenses.GroupBy(t => t.CategoryID)
                .Select(g => new { Category = categories.ContainsKey(g.Key) ? categories[g.Key] : "Uncategorised", Total = g.Sum(t => t.Amount) })
                .OrderByDescending(x => x.Total).ToList();
            decimal grandTotal = grouped.Sum(g => g.Total);
            if (grandTotal == 0) return;
            double cx = 100, cy = 100, radius = 90, startAngle = -90;
            for (int i = 0; i < grouped.Count; i++)
            {
                double sliceAngle = (double)(grouped[i].Total / grandTotal) * 360;
                double endAngle = startAngle + sliceAngle;
                var figure = new PathFigure { StartPoint = new Point(cx, cy) };
                figure.Segments.Add(new LineSegment(GetCirclePoint(cx, cy, radius, startAngle), true));
                figure.Segments.Add(new ArcSegment(GetCirclePoint(cx, cy, radius, endAngle), new Size(radius, radius), 0, sliceAngle > 180, SweepDirection.Clockwise, true));
                figure.Segments.Add(new LineSegment(new Point(cx, cy), true));
                var geo = new PathGeometry(); geo.Figures.Add(figure);
                PieCanvas.Children.Add(new Path { Fill = paletteBrushes[i % paletteBrushes.Length], Data = geo });
                startAngle = endAngle;
                if (i < 6)
                {
                    double pct = (double)(grouped[i].Total / grandTotal) * 100;
                    var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
                    row.Children.Add(new Rectangle { Width = 11, Height = 11, Fill = paletteBrushes[i % paletteBrushes.Length], Margin = new Thickness(0, 4, 8, 0), RadiusX = 2, RadiusY = 2 });
                    var lbl = new StackPanel();
                    lbl.Children.Add(new TextBlock { Text = grouped[i].Category, FontSize = 12, Foreground = textBrush });
                    lbl.Children.Add(new TextBlock { Text = $"€{grouped[i].Total:N2} · {pct:N1}%", FontSize = 11, Foreground = mutedBrush });
                    row.Children.Add(lbl);
                    PieLegend.Children.Add(row);
                }
            }
        }

        private void LoadTopCategories(List<Transaction> txs, Dictionary<int, string> categories)
        {
            TopCategoriesPanel.Children.Clear();
            var expenses = txs.Where(t => t.Type == "Expense").ToList();
            if (expenses.Count == 0) { TopCategoriesEmpty.Visibility = Visibility.Visible; return; }
            TopCategoriesEmpty.Visibility = Visibility.Collapsed;
            var grouped = expenses.GroupBy(t => t.CategoryID)
                .Select(g => new { Name = categories.ContainsKey(g.Key) ? categories[g.Key] : "Uncategorised", Total = g.Sum(t => t.Amount) })
                .OrderByDescending(x => x.Total).Take(5).ToList();
            decimal maxTotal = grouped.Max(x => x.Total);
            if (maxTotal == 0) maxTotal = 1;
            for (int i = 0; i < grouped.Count; i++)
            {
                var row = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
                var header = new Grid();
                header.Children.Add(new TextBlock { Text = grouped[i].Name, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = textBrush });
                header.Children.Add(new TextBlock { Text = $"€{grouped[i].Total:N2}", FontSize = 13, Foreground = textBrush, HorizontalAlignment = HorizontalAlignment.Right });
                row.Children.Add(header);
                double pct = (double)(grouped[i].Total / maxTotal);
                var barTrack = new Border { Background = new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB)), CornerRadius = new CornerRadius(4), Height = 8, Margin = new Thickness(0, 8, 0, 0) };
                var barTrackGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
                var barFill = new Border { Background = paletteBrushes[i % paletteBrushes.Length], CornerRadius = new CornerRadius(4), Height = 8, HorizontalAlignment = HorizontalAlignment.Left };
                barTrack.SizeChanged += (s, ev) => barFill.Width = ev.NewSize.Width * pct;
                barTrackGrid.Children.Add(barFill);
                barTrack.Child = barTrackGrid;
                row.Children.Add(barTrack);
                TopCategoriesPanel.Children.Add(row);
            }
        }

        private void LoadTopTransactions(List<Transaction> txs, Dictionary<int, string> categories)
        {
            var expenses = txs.Where(t => t.Type == "Expense")
                .OrderByDescending(t => t.Amount).Take(10)
                .Select(t => new {
                    DateDisplay = t.Date.ToString("yyyy-MM-dd"),
                    Description = string.IsNullOrWhiteSpace(t.Description) ? "(no description)" : t.Description,
                    Category = categories.ContainsKey(t.CategoryID) ? categories[t.CategoryID] : "Uncategorised",
                    AmountDisplay = $"-€{t.Amount:N2}"
                }).ToList();
            TopTxGrid.ItemsSource = expenses;
            TopTxEmpty.Visibility = expenses.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            TopTxGrid.Visibility = expenses.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_user == null) return;
            try
            {
                var (m, y) = GetSelectedPeriod();
                new UserReport(_user.UserID, _user.Username).GeneratePDF(m, y);
            }
            catch (Exception ex) { MessageBox.Show("Export PDF failed: " + ex.Message); }
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_user == null) return;
            try
            {
                var (m, y) = GetSelectedPeriod();
                new UserReport(_user.UserID, _user.Username).GenerateExcel(m, y);
            }
            catch (Exception ex) { MessageBox.Show("Export Excel failed: " + ex.Message); }
        }

        private static void AddCanvasText(Canvas canvas, string text, double x, double y, Brush brush, double size)
        {
            var tb = new TextBlock { Text = text, Foreground = brush, FontSize = size };
            Canvas.SetLeft(tb, x); Canvas.SetTop(tb, y);
            canvas.Children.Add(tb);
        }

        private static Point GetCirclePoint(double cx, double cy, double r, double angleDeg)
        {
            double rad = angleDeg * Math.PI / 180.0;
            return new Point(cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
        }
    }
}