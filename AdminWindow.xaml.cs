using System;
using System.Collections.Generic;
using System.IO;
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

	// Simple class to hold phone country info for the dropdown
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

		private static readonly List<PhoneCountry> _countries = BuildCountryList();

		private static List<PhoneCountry> BuildCountryList()
		{
			// Pre-sorted by country code
			List<PhoneCountry> countries = new List<PhoneCountry>();
			countries.Add(new PhoneCountry("🇦🇺", "+61", "AU"));
			countries.Add(new PhoneCountry("🇧🇪", "+32", "BE"));
			countries.Add(new PhoneCountry("🇧🇷", "+55", "BR"));
			countries.Add(new PhoneCountry("🇨🇦", "+1",  "CA"));
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
			countries.Add(new PhoneCountry("🇺🇸", "+1",  "US"));
			return countries;
		}

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

		private void Refresh_Click(object sender, RoutedEventArgs e)
		{
			LoadStats();
			LoadUsers();
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

				string report =
					"================================================" + Environment.NewLine +
					"           FINANCY SYSTEM REPORT                " + Environment.NewLine +
					"================================================" + Environment.NewLine +
					"Generated:         " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
					"Generated by:      " + generatedBy + Environment.NewLine +
					"------------------------------------------------" + Environment.NewLine +
					"USER STATISTICS" + Environment.NewLine +
					"------------------------------------------------" + Environment.NewLine +
					"Total Users:       " + total + Environment.NewLine +
					"Active Users:      " + active + Environment.NewLine +
					"Inactive Users:    " + inactive + Environment.NewLine +
					"------------------------------------------------" + Environment.NewLine +
					"SYSTEM STATISTICS" + Environment.NewLine +
					"------------------------------------------------" + Environment.NewLine +
					"Total Accounts:    " + accounts + Environment.NewLine +
					"Total Transactions:" + transactions + Environment.NewLine +
					"================================================" + Environment.NewLine;

				string path = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					"Downloads",
					"FinancyReport_" + DateTime.Now.ToString("yyyy-MM-dd_HHmm") + ".txt"
				);

				File.WriteAllText(path, report);

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
			if (CurrentUser == null)
			{
				return;
			}

			try
			{
				_currentUserProfile = db.GetProfileByUserId(CurrentUser.UserID);

				if (_currentUserProfile == null)
				{
					_currentUserProfile = new UserProfile
					{
						UserID = CurrentUser.UserID,
						FirstName = "",
						LastName = "",
						PhoneNumber = "",
						AvatarUrl = "",
						PreferredCurrency = "USD"
					};
				}

				ProfileFirstName.Text = _currentUserProfile.FirstName != null ? _currentUserProfile.FirstName : "";
				ProfileLastName.Text = _currentUserProfile.LastName != null ? _currentUserProfile.LastName : "";
				AdminProfilePhone.Text = _currentUserProfile.PhoneNumber != null ? _currentUserProfile.PhoneNumber : "";
				ProfileEmail.Text = CurrentUser.Email;
				ProfileRole.Text = CurrentUser.Role.ToString();

				string initial = "A";
				if (CurrentUser.Username.Length > 0)
				{
					initial = CurrentUser.Username.Substring(0, 1).ToUpper();
				}

				TextBlock tb = ProfileAvatarButton.Template.FindName("AdminAvatarInitialText", ProfileAvatarButton) as TextBlock;
				if (tb != null)
				{
					tb.Text = initial;
				}

				AdminPhoneCountryPicker.ItemsSource = _countries;

				string stored = _currentUserProfile.PhoneNumber != null ? _currentUserProfile.PhoneNumber : "";
				PhoneCountry matched = null;
				foreach (PhoneCountry c in _countries)
				{
					if (stored.StartsWith(c.Dial))
					{
						matched = c;
						break;
					}
				}

				if (matched != null)
				{
					AdminPhoneCountryPicker.SelectedItem = matched;
					AdminProfilePhone.Text = stored.Substring(matched.Dial.Length).Trim();
				}
				else
				{
					AdminPhoneCountryPicker.SelectedIndex = 0;
				}

				ProfileErrorMessage.Visibility = Visibility.Collapsed;

				UsersView.Visibility = Visibility.Collapsed;
				CreateUserView.Visibility = Visibility.Collapsed;
				ProfileView.Visibility = Visibility.Visible;

				LoadAvatarImage(_currentUserProfile.AvatarUrl);
				SetActiveNav(NavProfile);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error loading profile: " + ex.Message,
					"Admin", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

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

			// Update the large profile view button
			ProfileAvatarButton.ApplyTemplate();
			System.Windows.Controls.Image image = ProfileAvatarButton.Template.FindName("AdminAvatarImage", ProfileAvatarButton) as System.Windows.Controls.Image;
			System.Windows.Controls.Border circle = ProfileAvatarButton.Template.FindName("AdminAvatarCircle", ProfileAvatarButton) as System.Windows.Controls.Border;
			if (image != null)
			{
				image.Source = bmp;
				image.Visibility = Visibility.Visible;
			}
			if (circle != null)
			{
				circle.Visibility = Visibility.Collapsed;
			}

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
		}

		private void BackToUsers_Click(object sender, RoutedEventArgs e)
		{
			ProfileView.Visibility = Visibility.Collapsed;
			CreateUserView.Visibility = Visibility.Collapsed;
			UsersView.Visibility = Visibility.Visible;
			SetActiveNav(NavUsers);
		}

		private void SetActiveNav(Button current)
		{
			NavUsers.Tag = null;
			NavCreateUser.Tag = null;
			NavProfile.Tag = null;
			NavReports.Tag = null;
			if (current != null)
			{
				current.Tag = "active";
			}
		}

		private void NavUsers_Click(object sender, RoutedEventArgs e)
		{
			ProfileView.Visibility = Visibility.Collapsed;
			CreateUserView.Visibility = Visibility.Collapsed;
			UsersView.Visibility = Visibility.Visible;
			SetActiveNav(NavUsers);
		}

		private void NavCreateUser_Click(object sender, RoutedEventArgs e)
		{
			CreateUser_Click(sender, e);
			SetActiveNav(NavCreateUser);
		}

		private void NavProfile_Click(object sender, RoutedEventArgs e)
		{
			AdminAvatar_Click(sender, e);
			SetActiveNav(NavProfile);
		}

		private void NavReports_Click(object sender, RoutedEventArgs e)
		{
			ExportReport_Click(sender, e);
		}

		private void AdminAvatarUpload_Click(object sender, RoutedEventArgs e)
		{
			if (CurrentUser == null || _currentUserProfile == null)
			{
				return;
			}

			Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
			dlg.Title = "Choose profile photo";
			dlg.Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files|*.*";

			if (dlg.ShowDialog() != true)
			{
				return;
			}

			try
			{
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string destDir = Path.Combine(appData, "FinancyApplication", "Avatars");
				Directory.CreateDirectory(destDir);
				string destPath = Path.Combine(destDir, "avatar_admin_" + CurrentUser.UserID + ".jpg");

				BitmapImage bmp = new BitmapImage();
				bmp.BeginInit();
				bmp.UriSource = new Uri(dlg.FileName, UriKind.Absolute);
				bmp.CacheOption = BitmapCacheOption.OnLoad;
				bmp.EndInit();

				JpegBitmapEncoder encoder = new JpegBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(bmp));
				using (FileStream fs = File.Create(destPath))
				{
					encoder.Save(fs);
				}

				_currentUserProfile.AvatarUrl = destPath.Replace("\\", "/");

				try
				{
					UserProfile existing = db.GetProfileByUserId(CurrentUser.UserID);
					if (existing != null)
					{
						db.UpdateProfile(_currentUserProfile);
					}
					else
					{
						db.InsertProfile(_currentUserProfile);
					}
				}
				catch (Exception)
				{
					// Non-fatal: avatar file is saved, DB will sync on next Save
				}

				LoadAvatarImage(destPath);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Could not save photo: " + ex.Message,
					"Admin", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void SaveProfile_Click(object sender, RoutedEventArgs e)
		{
			if (CurrentUser == null || _currentUserProfile == null)
			{
				return;
			}

			string firstName = "";
			if (ProfileFirstName.Text != null)
			{
				firstName = ProfileFirstName.Text.Trim();
			}

			string lastName = "";
			if (ProfileLastName.Text != null)
			{
				lastName = ProfileLastName.Text.Trim();
			}

			string localNumber = "";
			if (AdminProfilePhone.Text != null)
			{
				localNumber = AdminProfilePhone.Text.Trim();
			}

			string phone;
			PhoneCountry selectedCountry = AdminPhoneCountryPicker.SelectedItem as PhoneCountry;
			if (selectedCountry != null && !string.IsNullOrEmpty(localNumber))
			{
				phone = selectedCountry.Dial + " " + localNumber;
			}
			else
			{
				phone = localNumber;
			}

			_currentUserProfile.FirstName = firstName;
			_currentUserProfile.LastName = lastName;
			_currentUserProfile.PhoneNumber = phone;

			try
			{
				UserProfile existing = db.GetProfileByUserId(CurrentUser.UserID);
				if (existing != null)
				{
					db.UpdateProfile(_currentUserProfile);
				}
				else
				{
					db.InsertProfile(_currentUserProfile);
				}

				ProfileView.Visibility = Visibility.Collapsed;
				UsersView.Visibility = Visibility.Visible;
			}
			catch (Exception ex)
			{
				ProfileErrorMessage.Text = "Error saving profile: " + ex.Message;
				ProfileErrorMessage.Visibility = Visibility.Visible;
			}
		}

		private void CreateUser_Click(object sender, RoutedEventArgs e)
		{
			NewUsernameInput.Text = "";
			NewEmailInput.Text = "";
			NewPasswordInput.Password = "";
			NewConfirmPasswordInput.Password = "";
			NewRoleComboBox.SelectedIndex = 0;
			CreateUserError.Visibility = Visibility.Collapsed;
			CreateUserError.Text = "";

			UsersView.Visibility = Visibility.Collapsed;
			ProfileView.Visibility = Visibility.Collapsed;
			CreateUserView.Visibility = Visibility.Visible;
			SetActiveNav(NavCreateUser);
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
	}
}
