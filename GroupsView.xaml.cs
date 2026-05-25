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
                CheckPendingInvites();
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
                GroupsList.ItemsSource = groups;
                NoGroupsPanel.Visibility = groups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { NoGroupsPanel.Visibility = Visibility.Visible; }
            
            GroupSelectCombo.Items.Clear();
            foreach (var g in groups)
                GroupSelectCombo.Items.Add(new ComboBoxItem { Content = g.Name, Tag = g.GroupID });
            if (GroupSelectCombo.Items.Count > 0) GroupSelectCombo.SelectedIndex = 0;
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
                MessageBox.Show($"Group \"{group.Name}\" created!", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not create group: " + ex.Message);
            }
        }

        // ── Join Group ───────────────────────────────────────────────────────
        private void SendInvite_Click(object sender, RoutedEventArgs e)
        {
            string username = InviteUsernameInput.Text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Enter a username.", "Validation");
                return;
            }
            if (GroupSelectCombo.SelectedItem == null)
            {
                MessageBox.Show("Select a group to invite to.", "Validation");
                return;
            }

            try
            {
                int groupId = (int)(GroupSelectCombo.SelectedItem as ComboBoxItem).Tag;
                var targetUser = db.GetUserByUsername(username);
                if (targetUser == null)
                {
                    MessageBox.Show("User not found.", "Not Found");
                    return;
                }

                db.CreateGroupInvite(groupId, _currentUser.UserID, targetUser.UserID);
                InviteUsernameInput.Text = "";
                MessageBox.Show($"Invite sent to {username}!", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not send invite: " + ex.Message);
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
                CategoryName = db.GetCategoryName(t.CategoryID),
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
            AddExpenseModal.Visibility = Visibility.Visible;
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
                t.Create();

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
    }
}