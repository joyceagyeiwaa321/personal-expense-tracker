using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FinancyApplication
{
    public partial class GoalsView : UserControl
    {
        private readonly Data db = new Data();
        public User CurrentUser { get; set; }

        private List<Goal> _allGoals = new List<Goal>();

        // Brushes
        private readonly Brush textBrush = new SolidColorBrush(Color.FromRgb(31, 41, 55));
        private readonly Brush mutedBrush = new SolidColorBrush(Color.FromRgb(100, 116, 139));
        private readonly Brush trackBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235));
        private readonly Brush accentBrush = new SolidColorBrush(Color.FromRgb(0, 184, 148));        // #00B894
        private readonly Brush accentLightBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94));   // brighter green
        private readonly Brush badgeBgBrush = new SolidColorBrush(Color.FromRgb(220, 252, 231));     // very light green
        private readonly Brush badgeTextBrush = new SolidColorBrush(Color.FromRgb(22, 101, 52));     // dark green

        public GoalsView(User user)
        {
            InitializeComponent();
            CurrentUser = user;
            Loaded += GoalsView_Loaded;
        }

        // ── LOADED ────────────────────────────────────────────────────────

        private void GoalsView_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateFilters();
            RefreshPage();
        }

        private void PopulateFilters()
        {
            // Status filter
            StatusFilterComboBox.SelectionChanged -= Filter_SelectionChanged;
            StatusFilterComboBox.Items.Clear();
            StatusFilterComboBox.Items.Add(new ComboBoxItem { Content = "Status: All", Tag = "all" });
            StatusFilterComboBox.Items.Add(new ComboBoxItem { Content = "Ongoing", Tag = "ongoing" });
            StatusFilterComboBox.Items.Add(new ComboBoxItem { Content = "Completed", Tag = "completed" });
            StatusFilterComboBox.SelectedIndex = 0;
            StatusFilterComboBox.SelectionChanged += Filter_SelectionChanged;

            // Time Period filter
            PeriodFilterComboBox.SelectionChanged -= Filter_SelectionChanged;
            PeriodFilterComboBox.Items.Clear();
            PeriodFilterComboBox.Items.Add(new ComboBoxItem { Content = "Time Period: All", Tag = "all" });
            PeriodFilterComboBox.Items.Add(new ComboBoxItem { Content = "This Month", Tag = "month" });
            PeriodFilterComboBox.Items.Add(new ComboBoxItem { Content = "This Year", Tag = "year" });
            PeriodFilterComboBox.Items.Add(new ComboBoxItem { Content = "Past Deadline", Tag = "past" });
            PeriodFilterComboBox.SelectedIndex = 0;
            PeriodFilterComboBox.SelectionChanged += Filter_SelectionChanged;
        }

        // ── DATA LOAD ─────────────────────────────────────────────────────

        private void RefreshPage()
        {
            LoadGoals();
            ApplyFiltersAndRender();
        }

        private void LoadGoals()
        {
            _allGoals.Clear();
            if (CurrentUser == null) return;

            try
            {
                _allGoals = db.GetGoalsByUser(CurrentUser.UserID);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load goals: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<Goal> GetFilteredGoals()
        {
            string statusFilter = (StatusFilterComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "all";
            string periodFilter = (PeriodFilterComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "all";

            IEnumerable<Goal> filtered = _allGoals;

            // Status
            if (statusFilter == "ongoing")
                filtered = filtered.Where(g => !g.IsCompleted());
            else if (statusFilter == "completed")
                filtered = filtered.Where(g => g.IsCompleted());

            // Time period — applies only to goals that have a deadline
            DateTime now = DateTime.Now;
            if (periodFilter == "month")
            {
                filtered = filtered.Where(g => g.Deadline.HasValue
                    && g.Deadline.Value.Year == now.Year
                    && g.Deadline.Value.Month == now.Month);
            }
            else if (periodFilter == "year")
            {
                filtered = filtered.Where(g => g.Deadline.HasValue
                    && g.Deadline.Value.Year == now.Year);
            }
            else if (periodFilter == "past")
            {
                filtered = filtered.Where(g => g.Deadline.HasValue
                    && g.Deadline.Value.Date < now.Date
                    && !g.IsCompleted());
            }

            return filtered.ToList();
        }

        private void ApplyFiltersAndRender()
        {
            List<Goal> goals = GetFilteredGoals();
            RenderGoalCards(goals);
            DrawBarChart(goals);
            DrawPieChart(goals);
        }

        // ── RENDER GOAL CARDS ─────────────────────────────────────────────

        private void RenderGoalCards(List<Goal> goals)
        {
            GoalCardsPanel.Items.Clear();

            if (goals.Count == 0)
            {
                NoGoalsLabel.Visibility = Visibility.Visible;
                return;
            }

            NoGoalsLabel.Visibility = Visibility.Collapsed;

            foreach (Goal g in goals)
            {
                GoalCardsPanel.Items.Add(BuildGoalCard(g));
            }
        }

        private Border BuildGoalCard(Goal goal)
        {
            Border card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(18),
                Margin = new Thickness(0, 0, 14, 14),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 14,
                    ShadowDepth = 2,
                    Opacity = 0.08
                }
            };

            StackPanel content = new StackPanel();

            // Header row: name + actions
            Grid headerRow = new Grid();
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock nameText = new TextBlock
            {
                Text = goal.Name,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = textBrush,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameText, 0);
            headerRow.Children.Add(nameText);

            StackPanel actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            Button editBtn = new Button
            {
                Content = "✎",
                Width = 26,
                Height = 26,
                Margin = new Thickness(0, 0, 6, 0),
                Background = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 11,
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = goal
            };
            editBtn.Click += EditGoal_Click;
            actions.Children.Add(editBtn);

            Button delBtn = new Button
            {
                Content = "🗑",
                Width = 26,
                Height = 26,
                Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 11,
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = goal
            };
            delBtn.Click += DeleteGoal_Click;
            actions.Children.Add(delBtn);

            Grid.SetColumn(actions, 1);
            headerRow.Children.Add(actions);

            content.Children.Add(headerRow);

            // Target / Saved line
            TextBlock targetSaved = new TextBlock
            {
                Text = $"€ {goal.TargetAmount:N0} Target | € {goal.SavedAmount:N0} Saved",
                FontSize = 12,
                Foreground = mutedBrush,
                Margin = new Thickness(0, 8, 0, 12)
            };
            content.Children.Add(targetSaved);

            // Progress bar with percentage label on the right
            Grid progressGrid = new Grid { Height = 10, Margin = new Thickness(0, 4, 0, 0) };
            progressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            progressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid barWrap = new Grid { Height = 10 };
            Grid.SetColumn(barWrap, 0);

            Border barBg = new Border
            {
                Background = trackBrush,
                CornerRadius = new CornerRadius(5)
            };
            barWrap.Children.Add(barBg);

            Border barFill = new Border
            {
                Background = accentLightBrush,
                CornerRadius = new CornerRadius(5),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            double pctFraction = goal.GetProgressPercent() / 100.0;
            barWrap.Children.Add(barFill);

            barWrap.SizeChanged += (s, e) =>
            {
                barFill.Width = barWrap.ActualWidth * pctFraction;
            };
            barFill.Loaded += (s, e) =>
            {
                barFill.Width = barWrap.ActualWidth * pctFraction;
            };

            progressGrid.Children.Add(barWrap);

            TextBlock pctLabel = new TextBlock
            {
                Text = $"{goal.GetProgressPercent():F0}%",
                FontSize = 11,
                Foreground = mutedBrush,
                Margin = new Thickness(8, -3, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(pctLabel, 1);
            progressGrid.Children.Add(pctLabel);

            content.Children.Add(progressGrid);

            // Status badge
            Border badge = new Border
            {
                Background = badgeBgBrush,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 3, 10, 3),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 14, 0, 0)
            };
            badge.Child = new TextBlock
            {
                Text = goal.GetStatus(),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = badgeTextBrush
            };
            content.Children.Add(badge);

            card.Child = content;
            return card;
        }

        private ControlTemplate MakeRoundButtonTemplate(double radius)
        {
            // Builds a simple round-corner template programmatically.
            // We do this in code so action buttons keep their colored hover effect
            // without duplicating XAML styles.
            string xaml =
                "<ControlTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
                "TargetType=\"Button\">" +
                "<Border x:Name=\"bg\" Background=\"{TemplateBinding Background}\" " +
                $"CornerRadius=\"{radius}\">" +
                "<ContentPresenter HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\"/>" +
                "</Border>" +
                "<ControlTemplate.Triggers>" +
                "<Trigger Property=\"IsMouseOver\" Value=\"True\">" +
                "<Setter TargetName=\"bg\" Property=\"Opacity\" Value=\"0.85\"/>" +
                "</Trigger>" +
                "</ControlTemplate.Triggers>" +
                "</ControlTemplate>";
            return (ControlTemplate)System.Windows.Markup.XamlReader.Parse(xaml);
        }

        // ── BAR CHART: Goal Progress (€) ──────────────────────────────────

        private void DrawBarChart(List<Goal> goals)
        {
            GoalBarCanvas.Children.Clear();

            if (goals.Count == 0)
            {
                AddCanvasText(GoalBarCanvas, "No goal data yet", 20, 100, mutedBrush, 13);
                return;
            }

            double canvasWidth = GoalBarCanvas.ActualWidth > 0 ? GoalBarCanvas.ActualWidth : 440;
            double canvasHeight = 220;

            // Wait for size if not yet measured
            if (GoalBarCanvas.ActualWidth <= 0)
            {
                GoalBarCanvas.SizeChanged += BarCanvas_SizeChanged;
                return;
            }

            double leftPad = 50;
            double rightPad = 14;
            double topPad = 10;
            double bottomPad = 40;

            double plotW = canvasWidth - leftPad - rightPad;
            double plotH = canvasHeight - topPad - bottomPad;

            decimal maxValue = goals.Max(g => g.TargetAmount);
            if (maxValue <= 0) maxValue = 1;

            // Y-axis grid + labels (5 ticks)
            int ticks = 4;
            for (int i = 0; i <= ticks; i++)
            {
                double y = topPad + plotH - (plotH * i / ticks);
                decimal labelVal = maxValue * i / ticks;

                GoalBarCanvas.Children.Add(new Line
                {
                    X1 = leftPad,
                    Y1 = y,
                    X2 = leftPad + plotW,
                    Y2 = y,
                    Stroke = trackBrush,
                    StrokeThickness = 1
                });

                AddCanvasText(GoalBarCanvas, $"{(int)labelVal}", 4, y - 8, mutedBrush, 10);
            }

            // Bars — two bars per goal (Saved, Remaining), grouped
            int n = goals.Count;
            double groupWidth = plotW / n;
            double barW = Math.Min(20, groupWidth / 3);
            double gap = 4;

            for (int i = 0; i < n; i++)
            {
                Goal g = goals[i];
                double saved = Math.Min((double)g.SavedAmount, (double)maxValue);
                double remaining = Math.Max((double)maxValue * 0, (double)(g.TargetAmount - g.SavedAmount));
                if (remaining < 0) remaining = 0;
                // For the visualization, "Remaining" bar shows what's left up to target
                double remainingDisplay = Math.Min(remaining, (double)maxValue);

                double groupX = leftPad + i * groupWidth + (groupWidth - (barW * 2 + gap)) / 2;

                double savedH = plotH * (saved / (double)maxValue);
                double remainingH = plotH * (remainingDisplay / (double)maxValue);

                // Saved bar (green)
                Rectangle savedRect = new Rectangle
                {
                    Width = barW,
                    Height = savedH,
                    Fill = accentLightBrush,
                    RadiusX = 2,
                    RadiusY = 2
                };
                Canvas.SetLeft(savedRect, groupX);
                Canvas.SetTop(savedRect, topPad + plotH - savedH);
                GoalBarCanvas.Children.Add(savedRect);

                // Remaining bar (light gray)
                Rectangle remRect = new Rectangle
                {
                    Width = barW,
                    Height = remainingH,
                    Fill = trackBrush,
                    RadiusX = 2,
                    RadiusY = 2
                };
                Canvas.SetLeft(remRect, groupX + barW + gap);
                Canvas.SetTop(remRect, topPad + plotH - remainingH);
                GoalBarCanvas.Children.Add(remRect);

                // X-axis label (goal name) — truncated, can wrap to two lines
                string label = g.Name ?? "";
                string[] words = label.Split(' ');
                string line1 = "";
                string line2 = "";
                foreach (string w in words)
                {
                    if ((line1 + " " + w).Trim().Length <= 10)
                        line1 = (line1 + " " + w).Trim();
                    else
                        line2 = (line2 + " " + w).Trim();
                }

                double labelX = leftPad + i * groupWidth + (groupWidth / 2);
                AddCanvasText(GoalBarCanvas, line1,
                    labelX - (line1.Length * 3), topPad + plotH + 6, mutedBrush, 10);
                if (!string.IsNullOrEmpty(line2))
                {
                    AddCanvasText(GoalBarCanvas, line2,
                        labelX - (line2.Length * 3), topPad + plotH + 20, mutedBrush, 10);
                }
            }
        }

        private void BarCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            GoalBarCanvas.SizeChanged -= BarCanvas_SizeChanged;
            DrawBarChart(GetFilteredGoals());
        }

        // ── PIE CHART: Completed vs Ongoing ───────────────────────────────

        private void DrawPieChart(List<Goal> goals)
        {
            GoalPieCanvas.Children.Clear();
            PieLegendPanel.Children.Clear();

            int completed = goals.Count(g => g.IsCompleted());
            int ongoing = goals.Count - completed;
            int total = goals.Count;

            if (total == 0)
            {
                AddCanvasText(GoalPieCanvas, "No goals", 60, 90, mutedBrush, 13);
                return;
            }

            double cx = 95, cy = 95, radius = 85;
            double startAngle = -90;

            // We render two slices: Ongoing (green) and Completed (teal)
            Brush[] sliceBrushes = {
                new SolidColorBrush(Color.FromRgb(34, 197, 94)),    // bright green for Ongoing
                new SolidColorBrush(Color.FromRgb(20, 184, 166))    // teal for Completed
            };
            string[] sliceLabels = { "Ongoing", "Completed" };
            int[] sliceCounts = { ongoing, completed };

            for (int i = 0; i < sliceCounts.Length; i++)
            {
                if (sliceCounts[i] == 0) continue;

                double sliceAngle = sliceCounts[i] / (double)total * 360.0;
                double endAngle = startAngle + sliceAngle;

                // If a single slice covers everything, draw a full circle instead of an arc
                if (Math.Abs(sliceAngle - 360.0) < 0.001)
                {
                    Ellipse circle = new Ellipse
                    {
                        Width = radius * 2,
                        Height = radius * 2,
                        Fill = sliceBrushes[i]
                    };
                    Canvas.SetLeft(circle, cx - radius);
                    Canvas.SetTop(circle, cy - radius);
                    GoalPieCanvas.Children.Add(circle);

                    // Percentage label in center
                    AddCanvasText(GoalPieCanvas, "100%",
                        cx - 14, cy - 8, Brushes.White, 12);
                }
                else
                {
                    Point startPt = GetCirclePoint(cx, cy, radius, startAngle);
                    Point endPt = GetCirclePoint(cx, cy, radius, endAngle);

                    PathFigure figure = new PathFigure { StartPoint = new Point(cx, cy) };
                    figure.Segments.Add(new LineSegment(startPt, true));
                    figure.Segments.Add(new ArcSegment(endPt, new Size(radius, radius), 0,
                        sliceAngle > 180, SweepDirection.Clockwise, true));
                    figure.Segments.Add(new LineSegment(new Point(cx, cy), true));

                    PathGeometry geo = new PathGeometry();
                    geo.Figures.Add(figure);

                    GoalPieCanvas.Children.Add(new Path
                    {
                        Fill = sliceBrushes[i],
                        Data = geo
                    });

                    // Percentage label inside slice
                    double midAngle = startAngle + sliceAngle / 2;
                    Point labelPt = GetCirclePoint(cx, cy, radius * 0.6, midAngle);
                    string pctStr = $"{sliceCounts[i] * 100.0 / total:F1}%";
                    AddCanvasText(GoalPieCanvas, pctStr,
                        labelPt.X - 14, labelPt.Y - 8, Brushes.White, 12);
                }

                startAngle = endAngle;
            }

            // Legend
            for (int i = 0; i < sliceCounts.Length; i++)
            {
                StackPanel item = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 6, 0, 6)
                };
                Ellipse dot = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = sliceBrushes[i],
                    VerticalAlignment = VerticalAlignment.Center
                };
                TextBlock label = new TextBlock
                {
                    Text = sliceLabels[i],
                    FontSize = 12,
                    Foreground = textBrush,
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                item.Children.Add(dot);
                item.Children.Add(label);
                PieLegendPanel.Children.Add(item);
            }
        }

        // ── ADD / EDIT / DELETE ───────────────────────────────────────────

        private void AddGoal_Click(object sender, RoutedEventArgs e)
        {
            GoalDialog dlg = new GoalDialog(CurrentUser, null) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
            {
                RefreshPage();
            }
        }

        private void EditGoal_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Goal goal)
            {
                GoalDialog dlg = new GoalDialog(CurrentUser, goal) { Owner = Window.GetWindow(this) };
                if (dlg.ShowDialog() == true)
                {
                    RefreshPage();
                }
            }
        }

        private void DeleteGoal_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Goal goal)
            {
                MessageBoxResult confirm = MessageBox.Show(
                    $"Delete the goal '{goal.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        db.DeleteGoal(goal.GoalId);
                        RefreshPage();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not delete goal: " + ex.Message);
                    }
                }
            }
        }

        // ── EVENT HANDLERS ────────────────────────────────────────────────

        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) ApplyFiltersAndRender();
        }

        // ── HELPERS ───────────────────────────────────────────────────────

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