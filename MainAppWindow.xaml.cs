using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FinancyApplication
{
    public partial class MainAppWindow : Window
    {
        private User _currentUser;

        // Track which nav button is active so we can reset its style
        private Button _activeNavBtn;

        public MainAppWindow(User user)
        {
            InitializeComponent();
            _currentUser = user;
            Loaded += MainAppWindow_Loaded;
        }

        private void MainAppWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TopDate.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy");

            // Populate sidebar user info
            string name = _currentUser?.Username ?? "User";
            string email = _currentUser?.Email ?? "";
            SidebarUsername.Text = name;
            SidebarEmail.Text = email;
            AvatarInitial.Text = name.Length > 0 ? name[0].ToString().ToUpper() : "U";

            // Start on Dashboard
            _activeNavBtn = NavDashboard;
            ShowDashboard();
        }

        // ── Navigation ───────────────────────────────────────────────────────
        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            string page = btn.Tag?.ToString() ?? "Dashboard";

            // Update nav button styles
            SetActiveNav(btn);

            switch (page)
            {
                case "Dashboard":
                    PageTitle.Text = "Dashboard";
                    ShowDashboard();
                    break;

                case "Transactions":
                    PageTitle.Text = "Transactions";
                    PageHost.Content = new TransactionsView(_currentUser);
                    break;

                case "Accounts":
                    PageTitle.Text = "Accounts";
                    PageHost.Content = new AccountsView(_currentUser);
                    break;

                case "Budget":
                    PageTitle.Text = "Budget";
                    PageHost.Content = new BudgetPage(_currentUser);
                    break;

                case "Recurring":
                    // Recurring is a tab inside TransactionsView — navigate there
                    PageTitle.Text = "Transactions";
                    SetActiveNav(NavTransactions);
                    PageHost.Content = new TransactionsView(_currentUser);
                    break;

                // Group:
                case "Groups":
                    PageTitle.Text = "Groups";
                    PageHost.Content = new GroupsView(_currentUser);
                    break;

                case "Profile":
                    PageTitle.Text = "My Profile";
                    PageHost.Content = new ProfileView(_currentUser);
                    break;

                case "Reports":
                    PageTitle.Text = "Reports";
                    PageHost.Content = new ReportsView(_currentUser);
                    break;
            }
        }

        private void ShowDashboard()
        {
            // The Dashboard UserControl — embed it directly
            PageHost.Content = new DashboardView(_currentUser);
        }

        // Placeholder for pages not yet built
        private void ShowComingSoon(string pageName)
        {
            var panel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            panel.Children.Add(new TextBlock
            {
                Text = "🚧",
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            panel.Children.Add(new TextBlock
            {
                Text = pageName + " — Coming Soon",
                FontFamily = new FontFamily("Segoe UI Semibold"),
                FontSize = 18,
                Foreground = new SolidColorBrush(Color.FromRgb(26, 46, 34)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 12, 0, 0)
            });
            panel.Children.Add(new TextBlock
            {
                Text = "This page is under construction.",
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(143, 175, 159)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 6, 0, 0)
            });
            PageHost.Content = panel;
        }

        // ── Public navigation (called by child pages) ──────────────────────────
        public void NavigateTo(string page)
        {
            // Find the matching nav button and simulate a click
            Button btn = page switch
            {
                "Transactions" => NavTransactions,
                "Accounts" => NavAccounts,
                "Budget" => NavBudget,
                "Reports" => NavReports,
                "Groups" => NavGroups,
                "Profile" => NavProfile,
                _ => NavDashboard
            };
            btn.RaiseEvent(new System.Windows.RoutedEventArgs(
                System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        }

        // ── Nav style helper ─────────────────────────────────────────────────
        private void SetActiveNav(Button btn)
        {
            // Reset previous active button
            if (_activeNavBtn != null)
                _activeNavBtn.Style = (Style)FindResource("NavBtn");

            btn.Style = (Style)FindResource("NavBtnActive");
            _activeNavBtn = btn;
        }

        // ── Window chrome ────────────────────────────────────────────────────
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void Maximize_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized
                          ? WindowState.Normal : WindowState.Maximized;

        private void Exit_Click(object sender, RoutedEventArgs e) =>
            Application.Current.Shutdown();

        private void SignOut_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Properties["CurrentUser"] = null;
            new MainWindow().Show();
            Close();
        }
    }
}