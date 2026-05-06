using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace FinancyApplication
{
	public partial class ProfileWindow : Window
	{
		private Data db = new Data();
		private User _currentUser;
		private UserProfile _profile;

		public ProfileWindow(User user)
		{
			InitializeComponent();
			_currentUser = user;
			LoadProfile();
			PopulateCurrencyDropdown();
		}

		// ── LOAD & DISPLAY ────────────────────────────────────────────────

		private void LoadProfile()
		{
			_profile = db.GetProfileByUserId(_currentUser.UserID);

			if (_profile == null)
			{
				// Create a blank profile if one doesn't exist yet
				_profile = new UserProfile
				{
					UserID = _currentUser.UserID,
					FirstName = "",
					LastName = "",
					PhoneNumber = "",
					AvatarUrl = "",
					PreferredCurrency = "USD"
				};
				db.InsertProfile(_profile);
			}

			RefreshDisplayPanel();
		}

		private void RefreshDisplayPanel()
		{
			// Avatar initial — use first letter of first name or username
			string initial = !string.IsNullOrWhiteSpace(_profile.FirstName)
				? _profile.FirstName[0].ToString().ToUpper()
				: _currentUser.Username[0].ToString().ToUpper();
			AvatarInitialText.Text = initial;

			// Header card
			string fullName = _profile.GetFullName().Trim();
			DisplayName.Text = string.IsNullOrWhiteSpace(fullName) ? _currentUser.Username : fullName;
			DisplayEmail.Text = _currentUser.Email;
			DisplayRole.Text = _currentUser.Role.ToString();
			DisplayMemberSince.Text = _currentUser.CreatedAt.ToString("MMMM dd, yyyy");

			// Info panel
			InfoFirstName.Text = string.IsNullOrWhiteSpace(_profile.FirstName) ? "—" : _profile.FirstName;
			InfoLastName.Text = string.IsNullOrWhiteSpace(_profile.LastName) ? "—" : _profile.LastName;
			InfoUsername.Text = _currentUser.Username;
			InfoPhone.Text = string.IsNullOrWhiteSpace(_profile.PhoneNumber) ? "—" : _profile.PhoneNumber;
			InfoEmail.Text = _currentUser.Email;
			InfoCurrency.Text = string.IsNullOrWhiteSpace(_profile.PreferredCurrency) ? "—" : _profile.PreferredCurrency;

			// Always show InfoView on load / after save
			ShowInfoView();
		}

		// ── CURRENCY DROPDOWN ─────────────────────────────────────────────

		private void PopulateCurrencyDropdown()
		{
			var currencies = Account.GetCurrencies();
			EditCurrency.ItemsSource = currencies;

			// Pre-select the user's preferred currency
			if (!string.IsNullOrWhiteSpace(_profile?.PreferredCurrency))
			{
				var match = currencies.FirstOrDefault(c => c.StartsWith(_profile.PreferredCurrency));
				if (match != null)
					EditCurrency.SelectedItem = match;
			}

			if (EditCurrency.SelectedItem == null && currencies.Count > 0)
				EditCurrency.SelectedIndex = 0;
		}

		// ── VIEW SWITCHING ────────────────────────────────────────────────

		private void ShowInfoView()
		{
			InfoView.Visibility = Visibility.Visible;
			EditView.Visibility = Visibility.Collapsed;
			PasswordView.Visibility = Visibility.Collapsed;
		}

		private void ShowEditView()
		{
			// Pre-fill edit fields with current values
			EditFirstName.Text = _profile.FirstName;
			EditLastName.Text = _profile.LastName;
			EditUsername.Text = _currentUser.Username;
			EditPhone.Text = _profile.PhoneNumber;

			// Select currency in dropdown
			var currencies = EditCurrency.Items.Cast<string>().ToList();
			var match = currencies.FirstOrDefault(c => c.StartsWith(_profile.PreferredCurrency ?? ""));
			if (match != null) EditCurrency.SelectedItem = match;

			EditFeedback.Visibility = Visibility.Collapsed;

			InfoView.Visibility = Visibility.Collapsed;
			EditView.Visibility = Visibility.Visible;
			PasswordView.Visibility = Visibility.Collapsed;
		}

		private void ShowPasswordView()
		{
			CurrentPass.Clear();
			NewPass.Clear();
			ConfirmNewPass.Clear();
			PassStrengthBar.Value = 0;
			PassStrengthLabel.Text = "";
			PassFeedback.Visibility = Visibility.Collapsed;

			InfoView.Visibility = Visibility.Collapsed;
			EditView.Visibility = Visibility.Collapsed;
			PasswordView.Visibility = Visibility.Visible;
		}

		// ── BUTTON HANDLERS ───────────────────────────────────────────────

		private void EditProfile_Click(object sender, RoutedEventArgs e) => ShowEditView();
		private void ShowChangePassword_Click(object sender, RoutedEventArgs e) => ShowPasswordView();
		private void CancelEdit_Click(object sender, RoutedEventArgs e) => ShowInfoView();

		private void SaveProfile_Click(object sender, RoutedEventArgs e)
		{
			string firstName = EditFirstName.Text.Trim();
			string lastName = EditLastName.Text.Trim();
			string username = EditUsername.Text.Trim();
			string phone = EditPhone.Text.Trim();
			string currency = EditCurrency.SelectedItem?.ToString() ?? "";

			// Basic validation
			if (string.IsNullOrWhiteSpace(username))
			{
				ShowEditFeedback("Username cannot be empty.", isError: true);
				return;
			}

			if (!string.IsNullOrWhiteSpace(phone) && !Regex.IsMatch(phone, @"^\+?[\d\s\-().]{6,20}$"))
			{
				ShowEditFeedback("Please enter a valid phone number.", isError: true);
				return;
			}

			try
			{
				// Update profile
				_profile.FirstName = firstName;
				_profile.LastName = lastName;
				_profile.PhoneNumber = phone;
				_profile.PreferredCurrency = Account.ExtractCurrencyCode(currency);
				_profile.Save();

				// Update username on the user record if it changed
				// (Username is stored on the User table — use a direct query via Data)
				if (username != _currentUser.Username)
				{
					db.UpdateUsername(_currentUser.UserID, username);
					_currentUser.Username = username;
				}

				ShowEditFeedback("Profile saved successfully!", isError: false);
				RefreshDisplayPanel();
			}
			catch (Exception ex)
			{
				ShowEditFeedback("Error saving profile: " + ex.Message, isError: true);
			}
		}

		private void SavePassword_Click(object sender, RoutedEventArgs e)
		{
			string current = CurrentPass.Password;
			string newPw = NewPass.Password;
			string confirm = ConfirmNewPass.Password;

			if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(newPw))
			{
				ShowPassFeedback("Please fill in all password fields.", isError: true);
				return;
			}

			// Verify current password
			if (!db.ValidateLogin(_currentUser.Email, current))
			{
				ShowPassFeedback("Current password is incorrect.", isError: true);
				return;
			}

			if (!IsPasswordValid(newPw))
			{
				ShowPassFeedback("Password needs 8+ chars, uppercase, number & special character.", isError: true);
				return;
			}

			if (newPw != confirm)
			{
				ShowPassFeedback("Passwords do not match.", isError: true);
				return;
			}

			try
			{
				string hashed = BCrypt.Net.BCrypt.HashPassword(newPw);
				db.UpdateUserPassword(_currentUser.UserID, hashed);
				ShowPassFeedback("Password updated successfully!", isError: false);

				// Clear fields
				CurrentPass.Clear();
				NewPass.Clear();
				ConfirmNewPass.Clear();
				PassStrengthBar.Value = 0;
				PassStrengthLabel.Text = "";
			}
			catch (Exception ex)
			{
				ShowPassFeedback("Error updating password: " + ex.Message, isError: true);
			}
		}

		private void ExportData_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var report = new UserReport(_currentUser.UserID, _currentUser.Username);
				report.GenerateExcel(DateTime.Now.Month, DateTime.Now.Year);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Export failed: " + ex.Message);
			}
		}

		private void ClearAllData_Click(object sender, RoutedEventArgs e)
		{
			var result = MessageBox.Show(
				"Are you sure you want to clear all your transaction data? This cannot be undone.",
				"Clear All Data",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (result == MessageBoxResult.Yes)
			{
				try
				{
					var accounts = db.GetAccountsByUser(_currentUser.UserID);
					foreach (var acc in accounts)
					{
						var transactions = db.GetTransactionsByAccount(acc.AccountID);
						foreach (var t in transactions)
							db.DeleteTransaction(t.TransactionID);
					}
					MessageBox.Show("All transaction data has been cleared.");
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error clearing data: " + ex.Message);
				}
			}
		}

		private void Back_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		// ── PASSWORD STRENGTH ─────────────────────────────────────────────

		private void NewPass_PasswordChanged(object sender, RoutedEventArgs e)
		{
			UpdatePassStrengthUI(NewPass.Password);
		}

		private (int score, string label, Color color) GetPasswordStrength(string password)
		{
			if (string.IsNullOrEmpty(password)) return (0, "", Colors.Transparent);

			int score = 0;
			if (password.Length >= 8) score++;
			if (password.Length >= 12) score++;
			if (Regex.IsMatch(password, @"[A-Z]") && Regex.IsMatch(password, @"[a-z]")) score++;
			if (Regex.IsMatch(password, @"\d")) score++;
			if (Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]")) score++;

			int barScore = Math.Min(score, 4);

			return score switch
			{
				0 or 1 => (barScore, "Weak — add more characters", Color.FromRgb(239, 68, 68)),
				2 => (barScore, "Fair — add uppercase & numbers", Color.FromRgb(245, 158, 11)),
				3 => (barScore, "Good — add a special character", Color.FromRgb(59, 130, 246)),
				_ => (barScore, "Strong password ✓", Color.FromRgb(0, 184, 148)),
			};
		}

		private bool IsPasswordValid(string password)
		{
			return password.Length >= 8
				&& Regex.IsMatch(password, @"[A-Z]")
				&& Regex.IsMatch(password, @"[a-z]")
				&& Regex.IsMatch(password, @"\d")
				&& Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]");
		}

		private void UpdatePassStrengthUI(string password)
		{
			var (score, label, color) = GetPasswordStrength(password);
			PassStrengthBar.Value = score;
			PassStrengthLabel.Text = label;
			PassStrengthLabel.Foreground = new SolidColorBrush(color);

			// Update the bar fill color via the named PART_Indicator border
			var indicator = FindVisualChild<System.Windows.Controls.Border>(PassStrengthBar, "PART_Indicator");
			if (indicator != null)
				indicator.Background = new SolidColorBrush(color);
		}

		// ── FEEDBACK HELPERS ──────────────────────────────────────────────

		private void ShowEditFeedback(string msg, bool isError)
		{
			EditFeedback.Text = msg;
			EditFeedback.Foreground = isError
				? new SolidColorBrush(Color.FromRgb(220, 38, 38))
				: new SolidColorBrush(Color.FromRgb(5, 150, 105));
			EditFeedback.Visibility = Visibility.Visible;
		}

		private void ShowPassFeedback(string msg, bool isError)
		{
			PassFeedback.Text = msg;
			PassFeedback.Foreground = isError
				? new SolidColorBrush(Color.FromRgb(220, 38, 38))
				: new SolidColorBrush(Color.FromRgb(5, 150, 105));
			PassFeedback.Visibility = Visibility.Visible;
		}

		// ── VISUAL TREE HELPER ────────────────────────────────────────────

		private static T FindVisualChild<T>(DependencyObject parent, string name)
			where T : FrameworkElement
		{
			if (parent == null) return null;
			int count = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i);
				if (child is T t && t.Name == name) return t;
				var result = FindVisualChild<T>(child, name);
				if (result != null) return result;
			}
			return null;
		}
	}
}








