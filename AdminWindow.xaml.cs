using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FinancyApplication
{
	// Row bound to a single line in the Users DataGrid
	public class AdminUserRow
	{
		public int UserID { get; set; }
		public string Username { get; set; }
		public string Email { get; set; }
		public string Role { get; set; }
		public string Status { get; set; }
		public DateTime CreatedAt { get; set; }

		public string CreatedDisplay
		{
			get
			{
				if (CreatedAt == default(DateTime))
				{
					return "—";
				}
				return CreatedAt.ToString("dd MMM yyyy");
			}
		}

		public string StatusActionLabel { get; set; }
		public string RoleActionLabel { get; set; }
		public bool CanEdit { get; set; }
	}

	// Simple class to hold phone country info for the dropdown # NOT USED
	public class PhoneCountry
	{
		public string Flag { get; set; }
		public string Dial { get; set; }
		public string Code { get; set; }

		public PhoneCountry(string flag, string dial, string code)
		{
			Flag = flag;
			Dial = dial;
			Code = code;
		}
	}

	public partial class AdminWindow : Window
	{
		private readonly Data db = new Data();
		public User CurrentUser { get; set; }
		private List<User> _allUsers = new List<User>();
		private UserProfile _currentUserProfile;

		private static readonly List<PhoneCountry> _countries = BuildCountryList(); //NOT USED

		private static List<PhoneCountry> BuildCountryList()
		{
			// Pre-sorted by country code
			List<PhoneCountry> countries = new List<PhoneCountry>();
			countries.Add(new PhoneCountry("🇦🇺", "+61", "AU"));
			countries.Add(new PhoneCountry("🇧🇪", "+32", "BE"));
			countries.Add(new PhoneCountry("🇧🇷", "+55", "BR"));
			countries.Add(new PhoneCountry("🇨🇦", "+1", "CA"));
			countries.Add(new PhoneCountry("🇨🇳", "+86", "CN"));
			countries.Add(new PhoneCountry("🇩🇪", "+49", "DE"));
			countries.Add(new PhoneCountry("🇪🇸", "+34", "ES"));
			countries.Add(new PhoneCountry("🇫🇷", "+33", "FR"));
			countries.Add(new PhoneCountry("🇬🇧", "+44", "GB"));
			countries.Add(new PhoneCountry("🇮🇳", "+91", "IN"));
			countries.Add(new PhoneCountry("🇮🇹", "+39", "IT"));
			countries.Add(new PhoneCountry("🇯🇵", "+81", "JP"));
			countries.Add(new PhoneCountry("🇲🇽", "+52", "MX"));
			countries.Add(new PhoneCountry("🇳🇱", "+31", "NL"));
			countries.Add(new PhoneCountry("🇳🇴", "+47", "NO"));
			countries.Add(new PhoneCountry("🇳🇿", "+64", "NZ"));
			countries.Add(new PhoneCountry("🇸🇪", "+46", "SE"));
			countries.Add(new PhoneCountry("🇺🇸", "+1", "US"));
			return countries;
		} //NOT USED

		public AdminWindow()
		{
			InitializeComponent();
			Loaded += AdminWindow_Loaded;
		}

		private void AdminWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (CurrentUser == null && Application.Current.Properties.Contains("CurrentUser"))
			{
				CurrentUser = Application.Current.Properties["CurrentUser"] as User;
			}

			if (CurrentUser != null && !string.IsNullOrWhiteSpace(CurrentUser.Username))
			{
				AdminGreeting.Text = "Hello, " + CurrentUser.Username;

				TextBlock tb = AdminAvatarButton.Template.FindName("AdminInitial", AdminAvatarButton) as TextBlock;
				if (tb != null)
				{
					tb.Text = CurrentUser.Username.Substring(0, 1).ToUpper();
				}

				UserProfile profile = db.GetProfileByUserId(CurrentUser.UserID);
				if (profile != null && !string.IsNullOrWhiteSpace(profile.AvatarUrl))
				{
					LoadAvatarImage(profile.AvatarUrl);
				}
			}

			LoadStats();
			LoadUsers();
		}

		private void LoadStats()
		{
			try
			{
				int total = db.GetUserCount();
				int active = db.GetActiveUserCount();
				int inactive = total - active;
				int accounts = db.GetTotalAccountCount();
				int transactions = db.GetTotalTransactionCount();

				KpiUsers.Text = total.ToString();
				KpiActive.Text = active.ToString();
				KpiInactive.Text = inactive.ToString();
				KpiAccounts.Text = accounts.ToString();
				KpiTransactions.Text = transactions.ToString();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Could not load system stats: " + ex.Message,
					"Admin", MessageBoxButton.OK, MessageBoxImage.Warning);

				KpiUsers.Text = "—";
				KpiActive.Text = "—";
				KpiInactive.Text = "—";
				KpiAccounts.Text = "—";
				KpiTransactions.Text = "—";
			}
		}

		private void LoadUsers()
		{
			try
			{
				_allUsers = db.GetAllUsers();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Could not load users: " + ex.Message,
					"Admin", MessageBoxButton.OK, MessageBoxImage.Warning);
				_allUsers = new List<User>();
			}

			ApplyFilters();
		}

		private void ApplyFilters()
		{
			string search = "";
			if (SearchBox != null && SearchBox.Text != null)
			{
				search = SearchBox.Text.Trim().ToLowerInvariant();
			}

			string roleFilter = "All roles";
			if (RoleFilter != null && RoleFilter.SelectedItem != null)
			{
				ComboBoxItem selectedRole = RoleFilter.SelectedItem as ComboBoxItem;
				if (selectedRole != null && selectedRole.Content != null)
				{
					roleFilter = selectedRole.Content.ToString();
				}
			}

			// Filter users
			List<User> filtered = new List<User>();
			foreach (User u in _allUsers)
			{
				bool matchesSearch = true;
				bool matchesRole = true;

				if (!string.IsNullOrEmpty(search))
				{
					string uName = u.Username != null ? u.Username.ToLowerInvariant() : "";
					string uEmail = u.Email != null ? u.Email.ToLowerInvariant() : "";
					if (!uName.Contains(search) && !uEmail.Contains(search))
					{
						matchesSearch = false;
					}
				}

				if (roleFilter == "Admins only" && u.Role != UserRole.Admin)
				{
					matchesRole = false;
				}
				else if (roleFilter == "Users only" && u.Role != UserRole.User)
				{
					matchesRole = false;
				}

				if (matchesSearch && matchesRole)
				{
					filtered.Add(u);
				}
			}

			// Sort by CreatedAt descending
			filtered.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));

			// Build rows
			List<AdminUserRow> rows = new List<AdminUserRow>();
			foreach (User u in filtered)
			{
				AdminUserRow row = new AdminUserRow();
				row.UserID = u.UserID;
				row.Username = u.Username;
				row.Email = u.Email;
				row.Role = u.Role.ToString();
				row.CreatedAt = u.CreatedAt;

				if (u.IsActive)
				{
					row.Status = "Active";
					row.StatusActionLabel = "Suspend";
				}
				else
				{
					row.Status = "Suspended";
					row.StatusActionLabel = "Activate";
				}

				if (u.Role == UserRole.Admin)
				{
					row.RoleActionLabel = "Make User";
				}
				else
				{
					row.RoleActionLabel = "Make Admin";
				}

				if (CurrentUser == null || u.UserID != CurrentUser.UserID)
				{
					row.CanEdit = true;
				}
				else
				{
					row.CanEdit = false;
				}

				rows.Add(row);
			}

			UsersGrid.ItemsSource = rows;

			if (rows.Count == 0)
			{
				UsersGrid.Visibility = Visibility.Collapsed;
				UsersEmpty.Visibility = Visibility.Visible;
			}
			else
			{
				UsersGrid.Visibility = Visibility.Visible;
				UsersEmpty.Visibility = Visibility.Collapsed;
			}

			if (rows.Count == 1)
			{
				UsersSubtitle.Text = "1 user matches the current filters.";
			}
			else
			{
				UsersSubtitle.Text = rows.Count + " users match the current filters.";
			}
		}

		private void ToggleStatus_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			if (b == null || b.Tag == null)
			{
				return;
			}
			int id = (int)b.Tag;
			User u = _allUsers.Find(x => x.UserID == id);
			if (u == null)
			{
				return;
			}

			if (CurrentUser != null && u.UserID == CurrentUser.UserID)
			{
				MessageBox.Show("You can't change your own status while signed in.",
					"Admin", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			bool newStatus = !u.IsActive;
			string verb = "";
			if (newStatus)
			{
				verb = "activate";
			}
			else
			{
				verb = "suspend";
			}

			MessageBoxResult confirm = MessageBox.Show(
				"Are you sure you want to " + verb + " \"" + u.Username + "\"?",
				"Confirm",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);
			if (confirm != MessageBoxResult.Yes)
			{
				return;
			}

			try
			{
				db.UpdateUserStatus(u.UserID, newStatus);
				LoadStats();
				LoadUsers();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Update failed: " + ex.Message,
					"Admin", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void ToggleRole_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			if (b == null || b.Tag == null)
			{
				return;
			}
			int id = (int)b.Tag;
			User u = _allUsers.Find(x => x.UserID == id);
			if (u == null)
			{
				return;
			}

			if (CurrentUser != null && u.UserID == CurrentUser.UserID)
			{
				MessageBox.Show("You can't change your own role while signed in.",
					"Admin", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			UserRole newRole;
			string verb;
			if (u.Role == UserRole.Admin)
			{
				newRole = UserRole.User;
				verb = "demote to User";
			}
			else
			{
				newRole = UserRole.Admin;
				verb = "promote to Admin";
			}

			MessageBoxResult confirm = MessageBox.Show(
				"Are you sure you want to " + verb + " \"" + u.Username + "\"?",
				"Confirm",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);
			if (confirm != MessageBoxResult.Yes)
			{
				return;
			}

			try
			{
				db.UpdateUserRole(u.UserID, newRole.ToString());
				LoadUsers();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Role change failed: " + ex.Message,
					"Admin", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void DeleteUser_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			if (b == null || b.Tag == null)
			{
				return;
			}
			int id = (int)b.Tag;
			User u = _allUsers.Find(x => x.UserID == id);
			if (u == null)
			{
				return;
			}

			if (CurrentUser != null && u.UserID == CurrentUser.UserID)
			{
				MessageBox.Show("You can't delete your own account while signed in.",
					"Admin", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			MessageBoxResult confirm = MessageBox.Show(
				"Permanently delete \"" + u.Username + "\"?\n\nThis removes the user record.",
				"Confirm Delete",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);
			if (confirm != MessageBoxResult.Yes)
			{
				return;
			}

			try
			{
				db.DeleteUser(u.UserID);
				LoadStats();
				LoadUsers();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Delete failed: " + ex.Message,
					"Admin", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!IsLoaded)
			{
				return;
			}
			ApplyFilters();
		}

		private void RoleFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!IsLoaded)
			{
				return;
			}
			ApplyFilters();
		}

		private void ExportReport_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				int total = db.GetUserCount();
				int active = db.GetActiveUserCount();
				int inactive = total - active;
				int accounts = db.GetTotalAccountCount();
				int transactions = db.GetTotalTransactionCount();

				string generatedBy = "admin";
				if (CurrentUser != null)
				{
					generatedBy = CurrentUser.Username;
				}

				string path = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					"Downloads",
					"FinancyAdminReport_" + DateTime.Now.ToString("yyyy-MM-dd_HHmm") + ".xlsx"
				);

				using (var workbook = new XLWorkbook())
				{
					var ws = workbook.Worksheets.Add("Admin Report");

					ws.Cell("A1").Value = "Financy - Admin Report";
					ws.Cell("A1").Style.Font.Bold = true;
					ws.Cell("A1").Style.Font.FontSize = 16;
					ws.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#2e7d32");

					ws.Cell("A2").Value = "Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
					ws.Cell("A3").Value = "Generated By: " + generatedBy;

					ws.Cell("A5").Value = "Metric";
					ws.Cell("B5").Value = "Value";
					var hdr = ws.Range("A5:B5");
					hdr.Style.Font.Bold = true;
					hdr.Style.Fill.BackgroundColor = XLColor.FromHtml("#2e7d32");
					hdr.Style.Font.FontColor = XLColor.White;

					var rows = new (string, object)[]
					{
						("Total Users",        total),
						("Active Users",       active),
						("Inactive Users",     inactive),
						("Total Accounts",     accounts),
						("Total Transactions", transactions)
					};

					int row = 6;
					foreach (var (label, val) in rows)
					{
						ws.Cell(row, 1).Value = label;
						ws.Cell(row, 2).Value = val.ToString();
						string bg = row % 2 == 0 ? "#e8f5e9" : "#ffffff";
						ws.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.FromHtml(bg);
						row++;
					}

					ws.Columns().AdjustToContents();
					workbook.SaveAs(path);
				}

				MessageBox.Show("Report saved to Downloads:" + Environment.NewLine + path,
					"Admin", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Report export failed: " + ex.Message,
					"Admin", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void AdminAvatar_Click(object sender, RoutedEventArgs e)
		{
		}     // NOT USED
		private void LoadAvatarImage(string path)
		{
			if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
			{
				return;
			}

			BitmapImage bmp = new BitmapImage();
			bmp.BeginInit();
			bmp.UriSource = new Uri(path, UriKind.Absolute);
			bmp.CacheOption = BitmapCacheOption.OnLoad;
			bmp.EndInit();

			// Update the nav bar avatar button
			AdminAvatarButton.ApplyTemplate();
			System.Windows.Controls.Image navImage = AdminAvatarButton.Template.FindName("AdminNavImage", AdminAvatarButton) as System.Windows.Controls.Image;
			System.Windows.Controls.Border navBg = AdminAvatarButton.Template.FindName("bg", AdminAvatarButton) as System.Windows.Controls.Border;
			if (navImage != null)
			{
				navImage.Source = bmp;
				navImage.Visibility = Visibility.Visible;
			}
			if (navBg != null)
			{
				navBg.Visibility = Visibility.Collapsed;
			}
		} // ONLY FROM DB

		private void BackToUsers_Click(object sender, RoutedEventArgs e)
		{
			HideAllViews();
			UsersView.Visibility = Visibility.Visible;
			SetActiveNav(NavUsers);
		}

		private void SetActiveNav(Button current)
		{
			NavUsers.Tag = null;
			NavReports.Tag = null;
			NavCategories.Tag = null;
			if (current != null)
			{
				current.Tag = "active";
			}
		}

		private void HideAllViews()
		{
			UsersView.Visibility = Visibility.Collapsed;
			CreateUserView.Visibility = Visibility.Collapsed;
			CategoriesAdminView.Visibility = Visibility.Collapsed;
		}

		private void NavUsers_Click(object sender, RoutedEventArgs e)
		{
			HideAllViews();
			UsersView.Visibility = Visibility.Visible;
			SetActiveNav(NavUsers);
		}

		private void NavReports_Click(object sender, RoutedEventArgs e)
		{
			ExportReport_Click(sender, e);
		}

		private void AdminAvatarUpload_Click(object sender, RoutedEventArgs e)
		{
		} //NOT USED

		private void SaveProfile_Click(object sender, RoutedEventArgs e)
		{
		} //NOT USED

		private void CreateUser_Click(object sender, RoutedEventArgs e)
		{
			NewUsernameInput.Text = "";
			NewEmailInput.Text = "";
			NewPasswordInput.Password = "";
			NewConfirmPasswordInput.Password = "";
			NewRoleComboBox.SelectedIndex = 0;
			CreateUserError.Visibility = Visibility.Collapsed;
			CreateUserError.Text = "";

			HideAllViews();
			CreateUserView.Visibility = Visibility.Visible;
		}

		private void CreateUserSubmit_Click(object sender, RoutedEventArgs e)
		{
			string username = "";
			if (NewUsernameInput.Text != null)
			{
				username = NewUsernameInput.Text.Trim();
			}

			string email = "";
			if (NewEmailInput.Text != null)
			{
				email = NewEmailInput.Text.Trim();
			}

			string password = NewPasswordInput.Password != null ? NewPasswordInput.Password : "";
			string confirmPassword = NewConfirmPasswordInput.Password != null ? NewConfirmPasswordInput.Password : "";

			string role = "User";
			ComboBoxItem roleItem = NewRoleComboBox.SelectedItem as ComboBoxItem;
			if (roleItem != null && roleItem.Content != null)
			{
				role = roleItem.Content.ToString();
			}

			CreateUserError.Visibility = Visibility.Collapsed;
			CreateUserError.Text = "";

			if (string.IsNullOrWhiteSpace(username))
			{
				ShowCreateUserError("Username is required.");
				return;
			}
			if (username.Length < 3)
			{
				ShowCreateUserError("Username must be at least 3 characters long.");
				return;
			}
			if (string.IsNullOrWhiteSpace(email))
			{
				ShowCreateUserError("Email is required.");
				return;
			}
			if (!email.Contains("@"))
			{
				ShowCreateUserError("Please enter a valid email address.");
				return;
			}

			try
			{
				if (db.EmailExists(email))
				{
					ShowCreateUserError("This email address is already registered.");
					return;
				}
			}
			catch (Exception ex)
			{
				ShowCreateUserError("Error checking email: " + ex.Message);
				return;
			}

			if (string.IsNullOrWhiteSpace(password))
			{
				ShowCreateUserError("Password is required.");
				return;
			}
			if (password.Length < 6)
			{
				ShowCreateUserError("Password must be at least 6 characters long.");
				return;
			}
			if (password != confirmPassword)
			{
				ShowCreateUserError("Passwords do not match.");
				return;
			}

			string hashedPassword;
			try
			{
				hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
			}
			catch (Exception ex)
			{
				ShowCreateUserError("Error hashing password: " + ex.Message);
				return;
			}

			try
			{
				User newUser = new User();
				newUser.Username = username;
				newUser.Email = email;
				newUser.IsActive = true;
				newUser.CreatedAt = DateTime.Now;

				if (role == "Admin")
				{
					newUser.Role = UserRole.Admin;
				}
				else
				{
					newUser.Role = UserRole.User;
				}

				db.InsertUser(newUser, hashedPassword);

				LoadStats();
				LoadUsers();
				CreateUserView.Visibility = Visibility.Collapsed;
				UsersView.Visibility = Visibility.Visible;
			}
			catch (Exception ex)
			{
				ShowCreateUserError("Error creating user: " + ex.Message);
			}
		}

		private void ShowCreateUserError(string message)
		{
			CreateUserError.Text = message;
			CreateUserError.Visibility = Visibility.Visible;
		}

		private void SendReset_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			if (b == null || b.Tag == null)
			{
				return;
			}
			int id = (int)b.Tag;
			User u = _allUsers.Find(x => x.UserID == id);
			if (u == null)
			{
				return;
			}

			MessageBoxResult confirm = MessageBox.Show(
				"Send password reset email to \"" + u.Username + "\" (" + u.Email + ")?",
				"Confirm",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);
			if (confirm != MessageBoxResult.Yes)
			{
				return;
			}

			try
			{
				bool success = db.ResendPasswordReset(u.Email, u.Username);
				if (success)
				{
					MessageBox.Show("Password reset email sent successfully!",
						"Admin", MessageBoxButton.OK, MessageBoxImage.Information);
				}
				else
				{
					MessageBox.Show("Failed to send password reset email. Please try again.",
						"Admin", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error sending reset email: " + ex.Message,
					"Admin", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void Logout_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Properties["CurrentUser"] = null;
			new MainWindow().Show();
			this.Close();
		}

		// ──────────────────────────────────────────
		// ADMIN CATEGORY MANAGEMENT
		// ──────────────────────────────────────────

		private List<Category> _adminCategories = new List<Category>();
		private Category _editingAdminCategory = null;

		private void NavCategories_Click(object sender, RoutedEventArgs e)
		{
			HideAllViews();
			CategoriesAdminView.Visibility = Visibility.Visible;
			SetActiveNav(NavCategories);
			LoadAdminCategories();
		}

		private void LoadAdminCategories()
		{
			try
			{
				// Deduplicate by Name+Type — DB seeds one row per user
				_adminCategories = db.GetDefaultCategories()
					.GroupBy(c => new { c.Name, c.Type })
					.Select(g => g.First())
					.ToList();
			}
			catch
			{
				_adminCategories = new List<Category>();
			}

			var income = _adminCategories
				.Where(c => string.Equals(c.Type, "Income", StringComparison.OrdinalIgnoreCase))
				.OrderBy(c => c.Name)
				.Select(c => new CategoryRow { CategoryID = c.CategoryID, Name = c.Name, Type = c.Type, IsDefault = c.IsDefault })
				.ToList();

			var expense = _adminCategories
				.Where(c => !string.Equals(c.Type, "Income", StringComparison.OrdinalIgnoreCase))
				.OrderBy(c => c.Name)
				.Select(c => new CategoryRow { CategoryID = c.CategoryID, Name = c.Name, Type = c.Type, IsDefault = c.IsDefault })
				.ToList();

			AdminIncomeList.ItemsSource = income;
			AdminExpenseList.ItemsSource = expense;
			AdminIncomeCount.Text = " (" + income.Count + ")";
			AdminExpenseCount.Text = " (" + expense.Count + ")";
		}

		private void AdminAddCategory_Click(object sender, RoutedEventArgs e)
		{
			_editingAdminCategory = null;
			AdminCategoryDialogTitle.Text = "Add Category";
			AdminCategorySaveBtn.Content = "Add Category";
			AdminCategoryName.Text = "";
			((ComboBoxItem)AdminCategoryType.Items[0]).IsSelected = true;
			AdminCategoryType.IsEnabled = true;
			AdminCategoryModal.Visibility = Visibility.Visible;
		}

		private void AdminEditCategory_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button b || b.Tag == null) return;
			int id = (int)b.Tag;
			_editingAdminCategory = _adminCategories.Find(x => x.CategoryID == id);
			if (_editingAdminCategory == null) return;

			AdminCategoryDialogTitle.Text = "Edit Category";
			AdminCategorySaveBtn.Content = "Update Category";
			AdminCategoryName.Text = _editingAdminCategory.Name;
			AdminCategoryType.IsEnabled = true;

			foreach (ComboBoxItem item in AdminCategoryType.Items)
			{
				if (item.Content.ToString().Equals(_editingAdminCategory.Type, StringComparison.OrdinalIgnoreCase))
				{
					AdminCategoryType.SelectedItem = item;
					break;
				}
			}

			AdminCategoryModal.Visibility = Visibility.Visible;
		}

		private void AdminDeleteCategory_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button b || b.Tag == null) return;
			int id = (int)b.Tag;
			Category cat = _adminCategories.Find(x => x.CategoryID == id);
			if (cat == null) return;

			var res = MessageBox.Show(
				"Delete the category \"" + cat.Name + "\"?" + Environment.NewLine + Environment.NewLine + "Any transactions using it will lose their category link.",
				"Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

			if (res == MessageBoxResult.Yes)
			{
				try { db.DeleteAllDefaultCategories(cat.Name, cat.Type); LoadAdminCategories(); }
				catch (Exception ex) { MessageBox.Show("Could not delete: " + ex.Message); }
			}
		}

		private void AdminCategorySave_Click(object sender, RoutedEventArgs e)
		{
			string name = AdminCategoryName.Text.Trim();
			if (string.IsNullOrWhiteSpace(name))
			{
				MessageBox.Show("Please enter a name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			string type = ((ComboBoxItem)AdminCategoryType.SelectedItem)?.Content?.ToString() ?? "Expense";

			try
			{
				if (_editingAdminCategory == null)
				{
					Category cat = new Category(CurrentUser?.UserID ?? 1, name, type);
					cat.IsDefault = true;
					cat.Create();
				}
				else
				{
					db.UpdateAllDefaultCategories(_editingAdminCategory.Name, _editingAdminCategory.Type, name, type);
				}

				AdminCategoryModal.Visibility = Visibility.Collapsed;
				LoadAdminCategories();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Could not save category: " + ex.Message);
			}
		}

		private void AdminCategoryCancel_Click(object sender, RoutedEventArgs e)
		{
			AdminCategoryModal.Visibility = Visibility.Collapsed;
		}
	}
}