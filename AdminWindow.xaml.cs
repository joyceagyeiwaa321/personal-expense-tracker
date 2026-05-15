using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FinancyApplication
{
	// Row VM bound to a single line in the Users DataGrid.
	// Strings are pre-computed so the grid can stay dumb and just bind by name.
	public class AdminUserRow
	{
		public int UserID { get; set; }
		public string Username { get; set; }
		public string Email { get; set; }
		public string Role { get; set; }          // "Admin" / "User"
		public string Status { get; set; }        // "Active" / "Suspended"
		public DateTime CreatedAt { get; set; }
		public string CreatedDisplay => CreatedAt == default ? "—" : CreatedAt.ToString("dd MMM yyyy");

		// "Suspend" if currently active, otherwise "Activate"
		public string StatusActionLabel { get; set; }

		// "Make Admin" if currently a User, otherwise "Make User"
		public string RoleActionLabel { get; set; }

		// False on the row representing the signed-in admin — protects them from
		// suspending / demoting / deleting themselves.
		public bool CanEdit { get; set; }
	}

	public partial class AdminWindow : Window
	{
		private readonly Data db = new Data();

		// The signed-in admin. Required so we can both (a) display their name in
		// the navbar and (b) prevent them mutating their own row in the table.
		public User CurrentUser { get; set; }

		// Last full load from the DB. The grid is rebuilt from this list every
		// time the search / role filter changes — keeps things simple and means
		// we don't need to round-trip the DB on each keystroke.
		private List<User> _allUsers = new List<User>();

		// Current admin's profile
		private UserProfile _currentUserProfile;

		// ── PHONE COUNTRY DATA ────────────────────────────────────────────────
		private record PhoneCountry(string Flag, string Dial, string Code);
		private static readonly List<PhoneCountry> _countries = BuildCountryList();

		private static List<PhoneCountry> BuildCountryList()
		{
			var countries = new List<PhoneCountry>
			{
				new("🇺🇸", "+1", "US"),
				new("🇬🇧", "+44", "GB"),
				new("🇨🇦", "+1", "CA"),
				new("🇦🇺", "+61", "AU"),
				new("🇩🇪", "+49", "DE"),
				new("🇫🇷", "+33", "FR"),
				new("🇮🇹", "+39", "IT"),
				new("🇪🇸", "+34", "ES"),
				new("🇲🇽", "+52", "MX"),
				new("🇧🇷", "+55", "BR"),
				new("🇮🇳", "+91", "IN"),
				new("🇯🇵", "+81", "JP"),
				new("🇨🇳", "+86", "CN"),
				new("🇳🇿", "+64", "NZ"),
				new("🇳🇱", "+31", "NL"),
				new("🇧🇪", "+32", "BE"),
				new("🇸🇪", "+46", "SE"),
				new("🇳🇴", "+47", "NO"),
			};
			return countries.OrderBy(c => c.Code).ToList();
		}

		public AdminWindow()
		{
			InitializeComponent();
			Loaded += AdminWindow_Loaded;
		}

		private void AdminWindow_Loaded(object sender, RoutedEventArgs e)
		{
			// Pull CurrentUser from Application.Properties if the caller didn't
			// set it explicitly — mirrors what Dashboard does.
			if (CurrentUser == null && Application.Current.Properties.Contains("CurrentUser"))
			{
				CurrentUser = Application.Current.Properties["CurrentUser"] as User;
			}

			if (CurrentUser != null && !string.IsNullOrWhiteSpace(CurrentUser.Username))
			{
				AdminGreeting.Text = "Hello, " + CurrentUser.Username;

				// Update avatar initial in the button template
				if (AdminAvatarButton.Template.FindName("AdminInitial", AdminAvatarButton) is TextBlock tb)
				{
					tb.Text = CurrentUser.Username.Substring(0, 1).ToUpper();
				}
			}

			LoadStats();
			LoadUsers();
		}

		// ── STATS STRIP ───────────────────────────────────────────────────

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

				KpiUsers.Text = KpiActive.Text = KpiInactive.Text = "—";
				KpiAccounts.Text = KpiTransactions.Text = "—";
			}
		}

		// ── USERS GRID ────────────────────────────────────────────────────

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

		// Rebuilds the rows on screen from _allUsers using the current search
		// box + role filter. Called from LoadUsers and from each filter event.
		private void ApplyFilters()
		{
			string search = SearchBox?.Text?.Trim().ToLowerInvariant() ?? "";
			string roleFilter = (RoleFilter?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All roles";

			IEnumerable<User> q = _allUsers;

			if (!string.IsNullOrEmpty(search))
			{
				q = q.Where(u =>
					(u.Username ?? "").ToLowerInvariant().Contains(search) ||
					(u.Email ?? "").ToLowerInvariant().Contains(search));
			}

			if (roleFilter == "Admins only")
			{
				q = q.Where(u => u.Role == UserRole.Admin);
			}
			else if (roleFilter == "Users only")
			{
				q = q.Where(u => u.Role == UserRole.User);
			}

			List<AdminUserRow> rows = q
				.OrderByDescending(u => u.CreatedAt)
				.Select(u => new AdminUserRow
				{
					UserID = u.UserID,
					Username = u.Username,
					Email = u.Email,
					Role = u.Role.ToString(),
					Status = u.IsActive ? "Active" : "Suspended",
					CreatedAt = u.CreatedAt,
					StatusActionLabel = u.IsActive ? "Suspend" : "Activate",
					RoleActionLabel = u.Role == UserRole.Admin ? "Make User" : "Make Admin",
					CanEdit = CurrentUser == null || u.UserID != CurrentUser.UserID
				})
				.ToList();

			UsersGrid.ItemsSource = rows;
			UsersGrid.Visibility = rows.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
			UsersEmpty.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

			UsersSubtitle.Text = rows.Count == 1
				? "1 user matches the current filters."
				: rows.Count + " users match the current filters.";
		}

		// ── ROW ACTIONS ───────────────────────────────────────────────────

		private void ToggleStatus_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button b || b.Tag == null) return;
			int id = (int)b.Tag;
			User u = _allUsers.Find(x => x.UserID == id);
			if (u == null) return;

			if (CurrentUser != null && u.UserID == CurrentUser.UserID)
			{
				MessageBox.Show("You can't change your own status while signed in.",
					"Admin", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			bool newStatus = !u.IsActive;
			string verb = newStatus ? "activate" : "suspend";

			MessageBoxResult confirm = MessageBox.Show(
				$"Are you sure you want to {verb} \"{u.Username}\"?",
				"Confirm",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);
			if (confirm != MessageBoxResult.Yes) return;

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
			if (sender is not Button b || b.Tag == null) return;
			int id = (int)b.Tag;
			User u = _allUsers.Find(x => x.UserID == id);
			if (u == null) return;

			if (CurrentUser != null && u.UserID == CurrentUser.UserID)
			{
				MessageBox.Show("You can't change your own role while signed in.",
					"Admin", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			UserRole newRole = u.Role == UserRole.Admin ? UserRole.User : UserRole.Admin;
			string verb = newRole == UserRole.Admin ? "promote to Admin" : "demote to User";

			MessageBoxResult confirm = MessageBox.Show(
				$"Are you sure you want to {verb} \"{u.Username}\"?",
				"Confirm",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);
			if (confirm != MessageBoxResult.Yes) return;

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
			if (sender is not Button b || b.Tag == null) return;
			int id = (int)b.Tag;
			User u = _allUsers.Find(x => x.UserID == id);
			if (u == null) return;

			if (CurrentUser != null && u.UserID == CurrentUser.UserID)
			{
				MessageBox.Show("You can't delete your own account while signed in.",
					"Admin", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			MessageBoxResult confirm = MessageBox.Show(
				$"Permanently delete \"{u.Username}\"?\n\n" +
				"This removes the user record. Related accounts / transactions may be left in the database.",
				"Confirm Delete",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);
			if (confirm != MessageBoxResult.Yes) return;

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

		// ── FILTER EVENTS ─────────────────────────────────────────────────

		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!IsLoaded) return;
			ApplyFilters();
		}

		private void RoleFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!IsLoaded) return;
			ApplyFilters();
		}

		// ── TOOLBAR ACTIONS ───────────────────────────────────────────────

		private void Refresh_Click(object sender, RoutedEventArgs e)
		{
			LoadStats();
			LoadUsers();
		}

		// Writes a plain-text system report to ~/Downloads. Equivalent to
		// Admin.GenerateReport() in UserRelated.cs but inlined so we can use
		// it from a User instance (the logged-in admin) without having to
		// rebuild the User as an Admin shim.
		private void ExportReport_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				int total = db.GetUserCount();
				int active = db.GetActiveUserCount();
				int inactive = total - active;
				int accounts = db.GetTotalAccountCount();
				int transactions = db.GetTotalTransactionCount();

				string generatedBy = CurrentUser?.Username ?? "admin";

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

		// ── AVATAR / PROFILE ──────────────────────────────────────────────

		private void AdminAvatar_Click(object sender, RoutedEventArgs e)
		{
			if (CurrentUser == null) return;

			// Load profile data
			try
			{
				_currentUserProfile = db.GetProfileByUserId(CurrentUser.UserID);

				if (_currentUserProfile == null)
				{
					// Create empty profile if doesn't exist
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

				// Populate profile form
				ProfileFirstName.Text = _currentUserProfile.FirstName ?? "";
				ProfileLastName.Text = _currentUserProfile.LastName ?? "";
				AdminProfilePhone.Text = _currentUserProfile.PhoneNumber ?? "";
				ProfileEmail.Text = CurrentUser.Email;
				ProfileRole.Text = CurrentUser.Role.ToString();

				string initial = CurrentUser.Username.Length > 0 ? CurrentUser.Username.Substring(0, 1).ToUpper() : "A";
				// Update avatar initial in button template
				if (ProfileAvatarButton.Template.FindName("AdminAvatarInitialText", ProfileAvatarButton) is TextBlock tb)
				{
					tb.Text = initial;
				}

				// Populate country code dropdown
				AdminPhoneCountryPicker.ItemsSource = _countries;

				// Try to pre-select the country code from the stored phone number
				string stored = _currentUserProfile.PhoneNumber ?? "";
				PhoneCountry matched = _countries.FirstOrDefault(c => stored.StartsWith(c.Dial));
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

				// If they already uploaded a photo before, show it
				LoadAvatarImage(_currentUserProfile.AvatarUrl);

				// Swap views: hide users, show profile
				UsersView.Visibility = Visibility.Collapsed;
				CreateUserView.Visibility = Visibility.Collapsed;
				ProfileView.Visibility = Visibility.Visible;
				SetActiveNav(NavProfile);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error loading profile: " + ex.Message,
					"Admin", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Pull the photo / circle / initial elements out of the avatar button template.
		// They're inside a ControlTemplate so they aren't reachable as ordinary x:Name fields.
		private void LoadAvatarImage(string path)
		{
			if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
				return;

			var image = ProfileAvatarButton.Template.FindName("AdminAvatarImage", ProfileAvatarButton) as System.Windows.Controls.Image;
			var circle = ProfileAvatarButton.Template.FindName("AdminAvatarCircle", ProfileAvatarButton) as System.Windows.Controls.Border;
			if (image == null || circle == null) return;

			var bmp = new BitmapImage();
			bmp.BeginInit();
			bmp.UriSource = new Uri(path, UriKind.Absolute);
			bmp.CacheOption = BitmapCacheOption.OnLoad;
			bmp.EndInit();

			image.Source = bmp;
			image.Visibility = Visibility.Visible;
			circle.Visibility = Visibility.Collapsed;
		}

		private void BackToUsers_Click(object sender, RoutedEventArgs e)
		{
			// Swap views: hide profile / create-user, show users
			ProfileView.Visibility = Visibility.Collapsed;
			CreateUserView.Visibility = Visibility.Collapsed;
			UsersView.Visibility = Visibility.Visible;
			SetActiveNav(NavUsers);
		}

		// ── SIDEBAR NAVIGATION ────────────────────────────────────────────

		// The NavItem ControlTemplate has a DataTrigger watching its own Tag for
		// the value "active" — so toggling Tag is enough to flip the highlight.
		private void SetActiveNav(Button current)
		{
			NavUsers.Tag = null;
			NavCreateUser.Tag = null;
			NavProfile.Tag = null;
			NavReports.Tag = null;
			if (current != null) current.Tag = "active";
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
			CreateUser_Click(sender, e); // reuses the existing reset + view-swap logic
			SetActiveNav(NavCreateUser);
		}

		private void NavProfile_Click(object sender, RoutedEventArgs e)
		{
			AdminAvatar_Click(sender, e); // reuses the existing profile-load logic
			SetActiveNav(NavProfile);
		}

		private void NavReports_Click(object sender, RoutedEventArgs e)
		{
			// Reports is an action, not a destination — fire the export and
			// leave the sidebar selection where it was.
			ExportReport_Click(sender, e);
		}

		// ── AVATAR UPLOAD ─────────────────────────────────────────────────

		private void AdminAvatarUpload_Click(object sender, RoutedEventArgs e)
		{
			if (CurrentUser == null || _currentUserProfile == null) return;

			var dlg = new Microsoft.Win32.OpenFileDialog
			{
				Title = "Choose profile photo",
				Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files|*.*"
			};

			if (dlg.ShowDialog() != true)
				return;

			try
			{
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string destDir = Path.Combine(appData, "FinancyApplication", "Avatars");
				Directory.CreateDirectory(destDir);
				string destPath = Path.Combine(destDir, $"avatar_admin_{CurrentUser.UserID}.jpg");

				// Decode the source file fully into memory before writing — otherwise
				// reusing the same destination filename can race with the file lock.
				var bmp = new BitmapImage();
				bmp.BeginInit();
				bmp.UriSource = new Uri(dlg.FileName, UriKind.Absolute);
				bmp.CacheOption = BitmapCacheOption.OnLoad;
				bmp.EndInit();

				var encoder = new JpegBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(bmp));
				using (var fs = File.Create(destPath))
				{
					encoder.Save(fs);
				}

				_currentUserProfile.AvatarUrl = destPath.Replace("\\", "/");

				// Persist right away so it survives a restart even if they
				// don't click "Save Changes".
				try
				{
					UserProfile existing = db.GetProfileByUserId(CurrentUser.UserID);
					if (existing != null)
						db.UpdateProfile(_currentUserProfile);
					else
						db.InsertProfile(_currentUserProfile);
				}
				catch
				{
					// Non-fatal: file is still on disk; SaveProfile will retry persistence.
				}

				// Show the new photo in the avatar button immediately.
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
			if (CurrentUser == null || _currentUserProfile == null) return;

			// Validate and collect data
			string firstName = ProfileFirstName.Text?.Trim() ?? "";
			string lastName = ProfileLastName.Text?.Trim() ?? "";
			string localNumber = AdminProfilePhone.Text?.Trim() ?? "";

			// Combine country dial code + local number, mirroring ProfileView
			string phone;
			var selectedCountry = AdminPhoneCountryPicker.SelectedItem as PhoneCountry;
			if (selectedCountry != null && !string.IsNullOrEmpty(localNumber))
				phone = $"{selectedCountry.Dial} {localNumber}";
			else
				phone = localNumber;

			// Update profile object
			_currentUserProfile.FirstName = firstName;
			_currentUserProfile.LastName = lastName;
			_currentUserProfile.PhoneNumber = phone;

			try
			{
				// Check if profile exists
				UserProfile existing = db.GetProfileByUserId(CurrentUser.UserID);

				if (existing != null)
				{
					db.UpdateProfile(_currentUserProfile);
				}
				else
				{
					db.InsertProfile(_currentUserProfile);
				}

				// Return to users view
				ProfileView.Visibility = Visibility.Collapsed;
				UsersView.Visibility = Visibility.Visible;
			}
			catch (Exception ex)
			{
				ProfileErrorMessage.Text = "Error saving profile: " + ex.Message;
				ProfileErrorMessage.Visibility = Visibility.Visible;
			}
		}

		// ── CREATE USER ───────────────────────────────────────────────────

		private void CreateUser_Click(object sender, RoutedEventArgs e)
		{
			// Reset the inline form
			NewUsernameInput.Text = "";
			NewEmailInput.Text = "";
			NewPasswordInput.Password = "";
			NewConfirmPasswordInput.Password = "";
			NewRoleComboBox.SelectedIndex = 0;
			CreateUserError.Visibility = Visibility.Collapsed;
			CreateUserError.Text = "";

			// Swap views: hide users, show create-user
			UsersView.Visibility = Visibility.Collapsed;
			ProfileView.Visibility = Visibility.Collapsed;
			CreateUserView.Visibility = Visibility.Visible;
			SetActiveNav(NavCreateUser);
		}

		private void CreateUserSubmit_Click(object sender, RoutedEventArgs e)
		{
			string username = NewUsernameInput.Text?.Trim() ?? "";
			string email = NewEmailInput.Text?.Trim() ?? "";
			string password = NewPasswordInput.Password ?? "";
			string confirmPassword = NewConfirmPasswordInput.Password ?? "";
			string role = (NewRoleComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "User";

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
				User newUser = new User
				{
					Username = username,
					Email = email,
					Role = role == "Admin" ? UserRole.Admin : UserRole.User,
					IsActive = true,
					CreatedAt = DateTime.Now
				};

				db.InsertUser(newUser, hashedPassword);

				// Refresh and return to users view (no popup — feedback is the new row showing up)
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

		// ── SEND PASSWORD RESET ───────────────────────────────────────────

		private void SendReset_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button b || b.Tag == null) return;
			int id = (int)b.Tag;
			User u = _allUsers.Find(x => x.UserID == id);
			if (u == null) return;

			MessageBoxResult confirm = MessageBox.Show(
				$"Send password reset email to \"{u.Username}\" ({u.Email})?",
				"Confirm",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);
			if (confirm != MessageBoxResult.Yes) return;

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

		// ── LOGOUT ────────────────────────────────────────────────────────

		private void Logout_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Properties["CurrentUser"] = null;
			new MainWindow().Show();
			this.Close();
		}
	}
}
