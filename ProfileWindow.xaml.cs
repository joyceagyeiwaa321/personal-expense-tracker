using System;
using System.Collections.Generic;
using System.Linq;
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
using PhoneNumbers; // ← requires NuGet: libphonenumber-csharp


namespace FinancyApplication
{
	public partial class ProfileView : UserControl
	{
		private Data db = new Data();
		private User _currentUser;
		private UserProfile _profile;

		// ── PHONE COUNTRY DATA ────────────────────────────────────────────────
		private record PhoneCountry(string Flag, string Dial, string Code);

		private static readonly List<PhoneCountry> _countries = BuildCountryList();

		private static List<PhoneCountry> BuildCountryList()
		{
			var util = PhoneNumberUtil.GetInstance();
			var countries = new List<PhoneCountry>();

			foreach (string regionCode in util.GetSupportedRegions())
			{
				int dialCode = util.GetCountryCodeForRegion(regionCode);
				string flag = RegionToFlag(regionCode);
				countries.Add(new PhoneCountry(flag, $"+{dialCode}", regionCode));
			}

			return countries.OrderBy(c => c.Code).ToList();
		}

		private static string RegionToFlag(string regionCode)
		{
			string flag = "";
			foreach (char c in regionCode)
				flag += char.ConvertFromUtf32(c + 0x1F1A5);
			return flag;
		}

		// ─────────────────────────────────────────────────────────────────────

		public ProfileView(User user)
		{
			InitializeComponent();
			_currentUser = user;
			LoadProfile();
			PopulateCurrencyDropdown();
			PopulatePhoneCountryPicker();
			Loaded += (s, e) => LoadAvatarImage(_profile?.AvatarUrl);

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
			string initial;
			if (!string.IsNullOrWhiteSpace(_profile.FirstName))
				initial = _profile.FirstName[0].ToString().ToUpper();
			else
				initial = _currentUser.Username[0].ToString().ToUpper();

			var avatarText = FindVisualChild<TextBlock>(AvatarButton, "AvatarInitialText");
			if (avatarText != null)
				avatarText.Text = initial; LoadAvatarImage(_profile?.AvatarUrl);

			// Header card
			string fullName = _profile.GetFullName().Trim();
			if (string.IsNullOrWhiteSpace(fullName))
				DisplayName.Text = _currentUser.Username;
			else
				DisplayName.Text = fullName;

			DisplayEmail.Text = _currentUser.Email;
			DisplayRole.Text = _currentUser.Role.ToString();

			// FIX: Only show role badge for Admin users
			if (_currentUser.Role == UserRole.Admin)
				RoleBadge.Visibility = Visibility.Visible;
			else
				RoleBadge.Visibility = Visibility.Collapsed;

			DisplayMemberSince.Text = _currentUser.CreatedAt.ToString("MMMM dd, yyyy");

			// Info panel
			if (string.IsNullOrWhiteSpace(_profile.FirstName))
				InfoFirstName.Text = "—";
			else
				InfoFirstName.Text = _profile.FirstName;

			if (string.IsNullOrWhiteSpace(_profile.LastName))
				InfoLastName.Text = "—";
			else
				InfoLastName.Text = _profile.LastName;

			InfoUsername.Text = _currentUser.Username;

			if (string.IsNullOrWhiteSpace(_profile.PhoneNumber))
				InfoPhone.Text = "—";
			else
				InfoPhone.Text = _profile.PhoneNumber;

			InfoEmail.Text = _currentUser.Email;

			if (string.IsNullOrWhiteSpace(_profile.PreferredCurrency))
				InfoCurrency.Text = "—";
			else
				InfoCurrency.Text = _profile.PreferredCurrency;

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

		// ── PHONE COUNTRY PICKER ──────────────────────────────────────────

		private void PopulatePhoneCountryPicker()
		{
			PhoneCountryPicker.ItemsSource = _countries;
			PhoneCountryPicker.SelectedIndex = 0;
		}

		private void PhoneCountryPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Nothing extra needed — the selected dial code is read on save
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
			EditFirstName.Text = _profile.FirstName;
			EditLastName.Text = _profile.LastName;
			EditUsername.Text = _currentUser.Username;

			// FIX: Split stored phone into country dial code + local number
			string stored = _profile.PhoneNumber;
			if (stored == null)
				stored = "";

			var matched = _countries.FirstOrDefault(c => stored.StartsWith(c.Dial));
			if (matched != null)
			{
				PhoneCountryPicker.SelectedItem = matched;
				EditPhone.Text = stored.Substring(matched.Dial.Length).Trim();
			}
			else
			{
				PhoneCountryPicker.SelectedIndex = 0;
				EditPhone.Text = stored;
			}

			// Select currency in dropdown
			var currencies = EditCurrency.Items.Cast<string>().ToList();
			var currencyMatch = currencies.FirstOrDefault(c => c.StartsWith(_profile.PreferredCurrency ?? ""));
			if (currencyMatch != null)
				EditCurrency.SelectedItem = currencyMatch;

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
			string currency = "";
			if (EditCurrency.SelectedItem != null)
				currency = EditCurrency.SelectedItem.ToString();

			// FIX: Combine country dial code + local number
			var selectedCountry = PhoneCountryPicker.SelectedItem as PhoneCountry;
			string localNumber = EditPhone.Text.Trim();
			string phone;

			if (selectedCountry != null && !string.IsNullOrEmpty(localNumber))
				phone = $"{selectedCountry.Dial} {localNumber}";
			else
				phone = localNumber;

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
				_profile.FirstName = firstName;
				_profile.LastName = lastName;
				_profile.PhoneNumber = phone;
				_profile.PreferredCurrency = Account.ExtractCurrencyCode(currency);
				_profile.Save();

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

			if (newPw == current)
			{
				ShowPassFeedback("New password can't be the same as your old one.", isError: true);
				return;
			}

			if (ContainsIdentity(newPw, _currentUser.Username, _profile?.FirstName, _profile?.LastName))
			{
				ShowPassFeedback("Password can't contain your username or name.", isError: true);
				return;
			}

			try
			{
				string hashed = BCrypt.Net.BCrypt.HashPassword(newPw);
				db.UpdateUserPassword(_currentUser.UserID, hashed);
				ShowPassFeedback("Password updated successfully!", isError: false);

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

		// ── AVATAR UPLOAD ─────────────────────────────────────────────────

		private void AvatarButton_Click(object sender, RoutedEventArgs e)
		{
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
				string destDir = System.IO.Path.Combine(appData, "FinancyApplication", "Avatars");
				System.IO.Directory.CreateDirectory(destDir);
				string destPath = System.IO.Path.Combine(destDir, $"avatar_{_currentUser.UserID}.jpg");

				var bmp = new BitmapImage(new Uri(dlg.FileName, UriKind.Absolute)); 
				var encoder = new JpegBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(bmp));
				using (var fs = System.IO.File.OpenWrite(destPath))
				{
					encoder.Save(fs);
				}

				_profile.AvatarUrl = destPath.Replace("\\", "/");
				_profile.Save();

				LoadAvatarImage(destPath);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Could not save photo: " + ex.Message);
			}
		}

		private void LoadAvatarImage(string path)
		{
			if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
				return;

			var image = FindVisualChild<Image>(AvatarButton, "AvatarImage");
			var circle = FindVisualChild<Border>(AvatarButton, "AvatarCircle");

			if (image == null || circle == null)
				return;

			var bmp = new BitmapImage();
			bmp.BeginInit();
			bmp.UriSource = new Uri(path, UriKind.Absolute);
			bmp.CacheOption = BitmapCacheOption.OnLoad;
			bmp.EndInit();

			image.Source = bmp;
			image.Visibility = Visibility.Visible;
			circle.Visibility = Visibility.Collapsed;
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

		// Rejects passwords that embed the user's identity (username / real name).
		private static bool ContainsIdentity(string password, params string[] tokens)
		{
			if (string.IsNullOrEmpty(password)) return false;
			string lower = password.ToLowerInvariant();
			foreach (string t in tokens)
			{
				if (string.IsNullOrWhiteSpace(t)) continue;
				string tl = t.Trim().ToLowerInvariant();
				if (tl.Length >= 3 && lower.Contains(tl)) return true;
			}
			return false;
		}

		private void UpdatePassStrengthUI(string password)
		{
			var (score, label, color) = GetPasswordStrength(password);
			PassStrengthBar.Value = score;
			PassStrengthLabel.Text = label;
			PassStrengthLabel.Foreground = new SolidColorBrush(color);

			var indicator = FindVisualChild<System.Windows.Controls.Border>(PassStrengthBar, "PART_Indicator");
			if (indicator != null)
				indicator.Background = new SolidColorBrush(color);
		}

		// ── FEEDBACK HELPERS ──────────────────────────────────────────────

		private void ShowEditFeedback(string msg, bool isError)
		{
			EditFeedback.Text = msg;
			if (isError)
				EditFeedback.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
			else
				EditFeedback.Foreground = new SolidColorBrush(Color.FromRgb(5, 150, 105));
			EditFeedback.Visibility = Visibility.Visible;
		}

		private void ShowPassFeedback(string msg, bool isError)
		{
			PassFeedback.Text = msg;
			if (isError)
				PassFeedback.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
			else
				PassFeedback.Foreground = new SolidColorBrush(Color.FromRgb(5, 150, 105));
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