using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FinancyApplication
{
    // ── View-models ──────────────────────────────────────────────────────────
    public class GroupExpenseRow
    {
        public int TransactionID { get; set; }
        public string DateDisplay { get; set; }
        public string Description { get; set; }
        public string CategoryName { get; set; }
        public string PaidBy { get; set; }
        public string AmountDisplay { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }

        public Brush StatusBg => Status == "Paid"
            ? new SolidColorBrush(Color.FromRgb(220, 252, 231))
            : new SolidColorBrush(Color.FromRgb(254, 226, 226));
        public Brush StatusFg => Status == "Paid"
            ? new SolidColorBrush(Color.FromRgb(22, 101, 52))
            : new SolidColorBrush(Color.FromRgb(153, 27, 27));
    }

    // ── Command helper ───────────────────────────────────────────────────────
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        public RelayCommand(Action<object> execute) => _execute = execute;
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged;
    }

    // ── GroupsView ───────────────────────────────────────────────────────────
    public partial class GroupsView : UserControl
    {
        private readonly Data db = new Data();
        private readonly User _currentUser;
        private Group _activeGroup;
        private List<Transaction> _groupTransactions = new List<Transaction>();
        private Dictionary<int, string> _categoryNames = new Dictionary<int, string>();
        private Dictionary<int, string> _userNames = new Dictionary<int, string>();

        // Tracks each row in the split picker so we can read values back
        private class SplitRowUI
        {
            public int UserID;
            public CheckBox Checkbox;
            public TextBox PercentBox;
        }
        private List<SplitRowUI> _splitRows = new List<SplitRowUI>();

        public ICommand OpenGroupCommand { get; }

        public GroupsView(User user)
        {
            InitializeComponent();
            _currentUser = user;
            DataContext = this;
            OpenGroupCommand = new RelayCommand(g => OpenGroup(g as Group));
            Loaded += (s, e) =>
            {
                LoadGroups();
            };
        }

        public void CheckPendingInvites()
        {
            try
            {
                var invites = db.GetPendingInvites(_currentUser.UserID);
                foreach (var inv in invites)
                {
                    var group = db.GetGroupById(inv.GroupID);
                    var from = db.GetUserById(inv.FromUserID);

                    var result = MessageBox.Show(
                        $"{from?.Username} invited you to join \"{group?.Name}\".\nDo you want to accept?",
                        "Group Invite",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    db.RespondToInvite(inv.InviteID, result == MessageBoxResult.Yes);

                    if (result == MessageBoxResult.Yes)
                    {
                        var member = new GroupMember(inv.GroupID, _currentUser.UserID);
                        member.Join();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load invites: " + ex.Message);
            }

            LoadGroups();
        }

        // ── Landing ──────────────────────────────────────────────────────────
        private void LoadGroups()
        {
            var groups = new List<Group>();
            try
            {
                groups = db.GetGroupsByUser(_currentUser.UserID);
                foreach (var g in groups)
                    g.MemberCount = db.GetGroupMembers(g.GroupID).Count;
            }
            catch { }

            // All groups you're a member of
            GroupsList.ItemsSource = groups;
            NoGroupsPanel.Visibility = groups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // Only groups you created
            var created = groups.Where(g => g.CreatedByUserID == _currentUser.UserID).ToList();
            CreatedGroupsList.ItemsSource = created;
            NoCreatedGroupsPanel.Visibility = created.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OpenGroup(Group group)
        {
            if (group == null) return;
            _activeGroup = group;

            GroupNameTitle.Text = group.Name;

            // Member count
            try
            {
                var members = db.GetGroupMembers(group.GroupID);
                GroupMembersLabel.Text = $"· {members.Count} member{(members.Count == 1 ? "" : "s")}";

                // Build username lookup
                _userNames.Clear();
                foreach (var m in members)
                {
                    var u = db.GetUserById(m.UserID);
                    if (u != null) _userNames[u.UserID] = u.Username;
                }
            }
            catch { }

            // Categories
            try
            {
                _categoryNames = db.GetAllCategoriesRaw();
            }
            catch { }

            PopulateCategoryFilter();
            LoadGroupExpenses();

            LandingView.Visibility = Visibility.Collapsed;
            GroupExpensesView.Visibility = Visibility.Visible;
        }

        private void BackToGroups_Click(object sender, RoutedEventArgs e)
        {
            GroupExpensesView.Visibility = Visibility.Collapsed;
            LandingView.Visibility = Visibility.Visible;
            _activeGroup = null;
            LoadGroups();
        }

        // ── Create Group ─────────────────────────────────────────────────────
        private void CreateGroup_Click(object sender, RoutedEventArgs e)
        {
            NewGroupName.Text = "";
            NewGroupDesc.Text = "";
            CreateGroupModal.Visibility = Visibility.Visible;
        }

        private void CancelCreate_Click(object sender, RoutedEventArgs e) =>
            CreateGroupModal.Visibility = Visibility.Collapsed;

        private void ConfirmCreate_Click(object sender, RoutedEventArgs e)
        {
            string name = NewGroupName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a group name.", "Validation");
                return;
            }
            try
            {
                var group = new Group(_currentUser.UserID, name, NewGroupDesc.Text.Trim());
                int groupId = group.Create();

                // Auto-join as member
                var member = new GroupMember(groupId, _currentUser.UserID);
                member.Join();

                CreateGroupModal.Visibility = Visibility.Collapsed;
                LoadGroups();
                GroupCodeText.Text = group.InviteCode;
                GroupCodeModal.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not create group: " + ex.Message);
            }
        }

        // ── Load Expenses ────────────────────────────────────────────────────
        private void LoadGroupExpenses()
        {
            if (_activeGroup == null) return;
            try
            {
                _groupTransactions = db.GetTransactionsByGroup(_activeGroup.GroupID);
            }
            catch { _groupTransactions = new List<Transaction>(); }
            ApplyFilters();
        }

        private void PopulateCategoryFilter()
        {
            CategoryFilter.Items.Clear();
            CategoryFilter.Items.Add(new ComboBoxItem { Content = "All Categories", IsSelected = true });
            foreach (var kv in _categoryNames)
                CategoryFilter.Items.Add(new ComboBoxItem { Content = kv.Value, Tag = kv.Key });
            CategoryFilter.SelectedIndex = 0;

            // Also populate the add-expense combo
            ExpenseCategoryCombo.Items.Clear();
            foreach (var kv in _categoryNames)
                ExpenseCategoryCombo.Items.Add(new ComboBoxItem { Content = kv.Value, Tag = kv.Key });
            if (ExpenseCategoryCombo.Items.Count > 0) ExpenseCategoryCombo.SelectedIndex = 0;

            // Account combo
            ExpenseAccountCombo.Items.Clear();
            var accounts = db.GetAccountsByUser(_currentUser.UserID);
            foreach (var a in accounts)
                ExpenseAccountCombo.Items.Add(new ComboBoxItem { Content = a.Name, Tag = a.AccountID });
            if (ExpenseAccountCombo.Items.Count > 0) ExpenseAccountCombo.SelectedIndex = 0;
        }

        private void ApplyFilters()
        {
            if (ExpensesTable == null) return;

            IEnumerable<Transaction> q = _groupTransactions;

            // Period
            if (PeriodFilter.SelectedItem is ComboBoxItem p)
            {
                DateTime now = DateTime.Today;
                if (p.Content.ToString() == "This Month")
                    q = q.Where(t => t.Date.Year == now.Year && t.Date.Month == now.Month);
                else if (p.Content.ToString() == "Last Month")
                {
                    var lm = now.AddMonths(-1);
                    q = q.Where(t => t.Date.Year == lm.Year && t.Date.Month == lm.Month);
                }
            }

            // Category
            if (CategoryFilter.SelectedItem is ComboBoxItem cat && cat.Tag != null)
                q = q.Where(t => t.CategoryID == (int)cat.Tag);

            // Type
            if (TypeFilter.SelectedItem is ComboBoxItem type && type.Content.ToString() != "All Types")
                q = q.Where(t => t.Type == type.Content.ToString());

            // Sort
            string sort = (SortFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Newest First";
            q = sort == "Oldest First" ? q.OrderBy(t => t.Date)
              : sort == "Highest Amount" ? q.OrderByDescending(t => t.Amount)
              : q.OrderByDescending(t => t.Date);

            var rows = q.Select(t => new GroupExpenseRow
            {
                TransactionID = t.TransactionID,
                DateDisplay = t.Date.ToString("dd MMM yyyy"),
                Description = string.IsNullOrWhiteSpace(t.Description) ? "—" : t.Description,
                CategoryName = string.IsNullOrEmpty(t.CategoryName) ? "—" : t.CategoryName,
                PaidBy = _userNames.ContainsKey(t.UserID) ? _userNames[t.UserID] : "Unknown",
                AmountDisplay = $"€{t.Amount:N2}",
                Amount = t.Amount,
                Status = t.Status ?? "Pending",
                Type = t.Type
            }).ToList();

            ExpensesTable.ItemsSource = rows;
            NoExpensesPanel.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            FooterCount.Text = $"Showing {rows.Count} expense{(rows.Count == 1 ? "" : "s")}";
        }

        private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilters();

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            PeriodFilter.SelectedIndex = 0;
            CategoryFilter.SelectedIndex = 0;
            TypeFilter.SelectedIndex = 0;
            SortFilter.SelectedIndex = 0;
            ApplyFilters();
        }

        // ── Add Expense ──────────────────────────────────────────────────────
        private void AddExpense_Click(object sender, RoutedEventArgs e)
        {
            ExpenseDesc.Text = "";
            ExpenseAmount.Text = "";
            BuildSplitRows();
            AddExpenseModal.Visibility = Visibility.Visible;
        }

        private void BuildSplitRows()
        {
            SplitMembersPanel.Children.Clear();
            _splitRows.Clear();
            SplitValidationMsg.Visibility = Visibility.Collapsed;

            if (_activeGroup == null) return;

            var members = db.GetGroupMembers(_activeGroup.GroupID);
            int n = members.Count;
            if (n == 0) return;

            decimal equalPct = Math.Round(100m / n, 2);

            foreach (var m in members)
            {
                string username = _userNames.ContainsKey(m.UserID) ? _userNames[m.UserID] : "User " + m.UserID;

                Grid row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                CheckBox cb = new CheckBox { IsChecked = true, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(cb, 0);
                row.Children.Add(cb);

                TextBlock name = new TextBlock
                {
                    Text = username,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0),
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(74, 101, 88))
                };
                Grid.SetColumn(name, 1);
                row.Children.Add(name);

                TextBox percent = new TextBox
                {
                    Text = equalPct.ToString("0.##"),
                    Height = 28,
                    FontSize = 12,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Padding = new Thickness(8, 0, 8, 0),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(224, 237, 230)),
                    BorderThickness = new Thickness(1)
                };
                Grid.SetColumn(percent, 2);
                row.Children.Add(percent);

                TextBlock pctLabel = new TextBlock
                {
                    Text = "%",
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 0, 0),
                    Foreground = new SolidColorBrush(Color.FromRgb(143, 175, 159))
                };
                Grid.SetColumn(pctLabel, 3);
                row.Children.Add(pctLabel);

                SplitMembersPanel.Children.Add(row);
                _splitRows.Add(new SplitRowUI { UserID = m.UserID, Checkbox = cb, PercentBox = percent });
            }
        }

        private void SplitEqual_Click(object sender, RoutedEventArgs e)
        {
            int checkedCount = _splitRows.Count(r => r.Checkbox.IsChecked == true);
            if (checkedCount == 0) return;

            decimal equalPct = Math.Round(100m / checkedCount, 2);
            foreach (var r in _splitRows)
                r.PercentBox.Text = r.Checkbox.IsChecked == true ? equalPct.ToString("0.##") : "0";
        }

        private void CancelExpense_Click(object sender, RoutedEventArgs e) =>
            AddExpenseModal.Visibility = Visibility.Collapsed;

        private void ConfirmExpense_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(ExpenseAmount.Text.Trim(),
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Enter a valid amount.", "Validation"); return;
            }
            if (string.IsNullOrWhiteSpace(ExpenseDesc.Text))
            {
                MessageBox.Show("Enter a description.", "Validation"); return;
            }
            if (ExpenseCategoryCombo.SelectedItem == null)
            {
                MessageBox.Show("Select a category.", "Validation"); return;
            }
            if (ExpenseAccountCombo.SelectedItem == null)
            {
                MessageBox.Show("You need an account first.", "Validation"); return;
            }

            // Split validation
            var checkedRows = _splitRows.Where(r => r.Checkbox.IsChecked == true).ToList();
            if (checkedRows.Count == 0)
            {
                SplitValidationMsg.Text = "Select at least one member to split with.";
                SplitValidationMsg.Visibility = Visibility.Visible;
                return;
            }

            decimal totalPct = 0;
            foreach (var r in checkedRows)
            {
                if (!decimal.TryParse(r.PercentBox.Text.Trim(),
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal pct) || pct < 0)
                {
                    SplitValidationMsg.Text = "Each percentage must be a number ≥ 0.";
                    SplitValidationMsg.Visibility = Visibility.Visible;
                    return;
                }
                totalPct += pct;
            }

            if (Math.Abs(totalPct - 100m) > 0.5m)
            {
                SplitValidationMsg.Text = $"Percentages must add up to 100% (currently {totalPct:0.##}%).";
                SplitValidationMsg.Visibility = Visibility.Visible;
                return;
            }
            SplitValidationMsg.Visibility = Visibility.Collapsed;

            try
            {
                int catId = (int)(ExpenseCategoryCombo.SelectedItem as ComboBoxItem).Tag;
                int accId = (int)(ExpenseAccountCombo.SelectedItem as ComboBoxItem).Tag;

                var t = new Transaction
                {
                    UserID = _currentUser.UserID,
                    AccountID = accId,
                    CategoryID = catId,
                    GroupID = _activeGroup.GroupID,
                    Type = "Expense",
                    Amount = amount,
                    Description = ExpenseDesc.Text.Trim(),
                    Date = DateTime.Now,
                    Status = "Pending"
                };
                if (!t.Create()) return;
                int txId = t.TransactionID;

                // Insert one split row per included member
                foreach (var r in checkedRows)
                {
                    decimal.TryParse(r.PercentBox.Text.Trim(),
                        System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out decimal pct);
                    decimal shareAmount = Math.Round(amount * pct / 100m, 2);
                    bool isPayer = r.UserID == _currentUser.UserID;

                    var split = new ExpenseSplit(txId, r.UserID, shareAmount, isPaid: isPayer);
                    db.InsertExpenseSplit(split);
                }

                AddExpenseModal.Visibility = Visibility.Collapsed;
                LoadGroupExpenses();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not add expense: " + ex.Message);
            }
        }

        // ── Mark Paid ────────────────────────────────────────────────────────
        private void MarkPaid_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b || b.Tag == null) return;
            int id = (int)b.Tag;
            try
            {
                // Find current status and toggle it
                var row = (ExpensesTable.ItemsSource as System.Collections.Generic.List<GroupExpenseRow>)
                          ?.FirstOrDefault(r => r.TransactionID == id);
                string newStatus = row?.Status == "Paid" ? "Pending" : "Paid";
                db.UpdateTransactionStatus(id, newStatus);
                LoadGroupExpenses();
            }
            catch (Exception ex) { MessageBox.Show("Could not update status: " + ex.Message); }
        }

        // ── Delete Expense ───────────────────────────────────────────────────
        private void DeleteExpense_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b || b.Tag == null) return;
            int id = (int)b.Tag;
            var res = MessageBox.Show("Delete this expense?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    db.DeleteTransaction(id);
                    LoadGroupExpenses();
                }
                catch (Exception ex) { MessageBox.Show("Could not delete: " + ex.Message); }
            }
        }

        // ── Settle Up ────────────────────────────────────────────────────────
        private void SettleUp_Click(object sender, RoutedEventArgs e)
        {
            var pending = _groupTransactions.Where(t => t.Status == "Pending").ToList();
            SettlePendingCount.Text = pending.Count.ToString();
            SettlePendingAmount.Text = $"€{pending.Sum(t => t.Amount):N2}";
            SettleUpModal.Visibility = Visibility.Visible;
        }

        private void CancelSettle_Click(object sender, RoutedEventArgs e) =>
            SettleUpModal.Visibility = Visibility.Collapsed;

        private void ConfirmSettle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var pending = _groupTransactions.Where(t => t.Status == "Pending").ToList();
                foreach (var t in pending)
                    db.UpdateTransactionStatus(t.TransactionID, "Paid");

                SettleUpModal.Visibility = Visibility.Collapsed;
                LoadGroupExpenses();
                MessageBox.Show($"Settled {pending.Count} expense(s)!", "Done");
            }
            catch (Exception ex) { MessageBox.Show("Could not settle: " + ex.Message); }
        }

        // ── Leave Group ──────────────────────────────────────────────────────
        private void LeaveGroup_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show($"Leave \"{_activeGroup.Name}\"?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    db.DeleteGroupMember(_activeGroup.GroupID, _currentUser.UserID);
                    BackToGroups_Click(null, null);
                    MessageBox.Show("You left the group.", "Done");
                }
                catch (Exception ex) { MessageBox.Show("Could not leave group: " + ex.Message); }
            }
        }

        // ── Copy Invite Code (from card) ─────────────────────────────────────
        private void CopyInviteCode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is string code && !string.IsNullOrEmpty(code))
            {
                Clipboard.SetText(code);
                MessageBox.Show("Invite code copied to clipboard!", "Copied");
            }
        }

        // ── Group Code Modal ──────────────────────────────────────────────────
        private void CopyGroupCode_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(GroupCodeText.Text))
            {
                Clipboard.SetText(GroupCodeText.Text);
                MessageBox.Show("Invite code copied to clipboard!", "Copied");
            }
        }

        private void CloseGroupCode_Click(object sender, RoutedEventArgs e) =>
            GroupCodeModal.Visibility = Visibility.Collapsed;

        // ── Join Group ────────────────────────────────────────────────────────
        private void JoinGroup_Click(object sender, RoutedEventArgs e)
        {
            JoinCodeInput.Text = "";
            JoinGroupModal.Visibility = Visibility.Visible;
        }

        private void CancelJoin_Click(object sender, RoutedEventArgs e) =>
            JoinGroupModal.Visibility = Visibility.Collapsed;

        private void ConfirmJoin_Click(object sender, RoutedEventArgs e)
        {
            string code = JoinCodeInput.Text.Trim().ToUpper();
            if (string.IsNullOrEmpty(code))
            {
                MessageBox.Show("Enter an invite code.", "Validation");
                return;
            }
            try
            {
                int groupId = Group.JoinByCode(_currentUser.UserID, code);
                if (groupId > 0)
                {
                    JoinGroupModal.Visibility = Visibility.Collapsed;
                    LoadGroups();
                    MessageBox.Show("You joined the group!", "Success");
                }
                else
                {
                    MessageBox.Show("Invalid invite code. Please check and try again.", "Not Found");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not join group: " + ex.Message);
            }
        }
    }
}