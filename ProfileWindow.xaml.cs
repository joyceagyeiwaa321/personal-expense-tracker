using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PhoneNumbers;

namespace FinancyApplication
{
	public partial class ProfileView : UserControl
	{
		private Data db = new Data();
		private User _currentUser;
		private UserProfile _profile;

		// Simple class for phone country dropdown
		private class PhoneCountry
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

		private static readonly List<PhoneCountry> _countries = BuildCountryList();

		private static List<PhoneCountry> BuildCountryList()
		{
			PhoneNumberUtil util = PhoneNumberUtil.GetInstance();
			List<PhoneCountry> countries = new List<PhoneCountry>();

			foreach (string regionCode in util.GetSupportedRegions())
			{
				int dialCode = util.GetCountryCodeForRegion(regionCode);
				string flag = RegionToFlag(regionCode);
				countries.Add(new PhoneCountry(flag, "+" + dialCode, regionCode));
			}

			countries.Sort((a, b) => string.Compare(a.Code, b.Code));
			return countries;
		}

		private static string RegionToFlag(string regionCode)
		{
			string flag = "";
			foreach (char c in regionCode)
			{
				flag += char.ConvertFromUtf32(c + 0x1F1A5);
			}
			return flag;
		}

		public ProfileView(User user)
		{
			InitializeComponent();
			_currentUser = user;
			LoadProfile();
			PopulateCurrencyDropdown();
			PopulatePhoneCountryPicker();
			Loaded += ProfileView_Loaded;
		}

		private void ProfileView_Loaded(object sender, RoutedEventArgs e)
		{
			LoadAvatarImage(_profile.AvatarUrl);
		}

		private void LoadProfile()
		{
			_profile = db.GetProfileByUserId(_currentUser.UserID);

			if (_profile == null)
			{
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
			string initial;
			if (!string.IsNullOrWhiteSpace(_profile.FirstName))
			{
				initial = _profile.FirstName[0].ToString().ToUpper();
			}
			else
			{
				initial = _currentUser.Username[0].ToString().ToUpper();
			}

			TextBlock avatarText = FindVisualChild<TextBlock>(AvatarButton, "AvatarInitialText");
			if (avatarText != null)
			{
				avatarText.Text = initial;
			}
			LoadAvatarImage(_profile.AvatarUrl);

			string fullName = _profile.GetFullName().Trim();
			if (string.IsNullOrWhiteSpace(fullName))
			{
				DisplayName.Text = _currentUser.Username;
			}
			else
			{
				DisplayName.Text = fullName;
			}

			DisplayEmail.Text = _currentUser.Email;
			DisplayRole.Text = _currentUser.Role.ToString();

			if (_currentUser.Role == UserRole.Admin)
			{
				RoleBadge.Visibility = Visibility.Visible;
			}
			else
			{
				RoleBadge.Visibility = Visibility.Collapsed;
			}

			DisplayMemberSince.Text = _currentUser.CreatedAt.ToString("MMMM dd, yyyy");

			if (string.IsNullOrWhiteSpace(_profile.FirstName))
			{
				InfoFirstName.Text = "—";
			}
			else
			{
				InfoFirstName.Text = _profile.FirstName;
			}

			if (string.IsNullOrWhiteSpace(_profile.LastName))
			{
				InfoLastName.Text = "—";
			}
			else
			{
				InfoLastName.Text = _profile.LastName;
			}

			InfoUsername.Text = _currentUser.Username;

			if (string.IsNullOrWhiteSpace(_profile.PhoneNumber))
			{
				InfoPhone.Text = "—";
			}
			else
			{
				InfoPhone.Text = _profile.PhoneNumber;
			}

			InfoEmail.Text = _currentUser.Email;

			if (string.IsNullOrWhiteSpace(_profile.PreferredCurrency))
			{
				InfoCurrency.Text = "—";
			}
			else
			{
				InfoCurrency.Text = _profile.PreferredCurrency;
			}

			ApiKeyInput.Text = OpenAiService.LoadApiKey();

			ShowInfoView();
            ChkGoalReminders.IsChecked = _profile.NotifGoalReminders;
        }

		private void PopulateCurrencyDropdown()
		{
			List<string> currencies = Account.GetCurrencies();
			EditCurrency.ItemsSource = currencies;

			if (!string.IsNullOrWhiteSpace(_profile.PreferredCurrency))
			{
				string match = null;
				foreach (string c in currencies)
				{
					if (c.StartsWith(_profile.PreferredCurrency))
					{
						match = c;
						break;
					}
				}
				if (match != null)
				{
					EditCurrency.SelectedItem = match;
				}
			}

			if (EditCurrency.SelectedItem == null && currencies.Count > 0)
			{
				EditCurrency.SelectedIndex = 0;
			}
		}

		private void PopulatePhoneCountryPicker()
		{
			PhoneCountryPicker.ItemsSource = _countries;
			PhoneCountryPicker.SelectedIndex = 0;
		}

		private void PhoneCountryPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Nothing extra needed — the selected dial code is read on save
		}

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

			string stored = _profile.PhoneNumber;
			if (stored == null)
			{
				stored = "";
			}

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
				PhoneCountryPicker.SelectedItem = matched;
				EditPhone.Text = stored.Substring(matched.Dial.Length).Trim();
			}
			else
			{
				PhoneCountryPicker.SelectedIndex = 0;
				EditPhone.Text = stored;
			}

			// Select currency in dropdown
			List<string> currencies = new List<string>();
			foreach (object item in EditCurrency.Items)
			{
				currencies.Add(item.ToString());
			}

			string prefCurrency = _profile.PreferredCurrency;
			if (prefCurrency == null)
			{
				prefCurrency = "";
			}

			string currencyMatch = null;
			foreach (string c in currencies)
			{
				if (c.StartsWith(prefCurrency))
				{
					currencyMatch = c;
					break;
				}
			}

			if (currencyMatch != null)
			{
				EditCurrency.SelectedItem = currencyMatch;
			}

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

		private void EditProfile_Click(object sender, RoutedEventArgs e)
		{
			ShowEditView();
		}

		private void ShowChangePassword_Click(object sender, RoutedEventArgs e)
		{
			ShowPasswordView();
		}

		private void CancelEdit_Click(object sender, RoutedEventArgs e)
		{
			ShowInfoView();
		}

		private void SaveProfile_Click(object sender, RoutedEventArgs e)
		{
			string firstName = EditFirstName.Text.Trim();
			string lastName = EditLastName.Text.Trim();
			string username = EditUsername.Text.Trim();

			string currency = "";
			if (EditCurrency.SelectedItem != null)
			{
				currency = EditCurrency.SelectedItem.ToString();
			}

			PhoneCountry selectedCountry = PhoneCountryPicker.SelectedItem as PhoneCountry;
			string localNumber = EditPhone.Text.Trim();
			string phone;

			if (selectedCountry != null && !string.IsNullOrEmpty(localNumber))
			{
				phone = selectedCountry.Dial + " " + localNumber;
			}
			else
			{
				phone = localNumber;
			}

			if (string.IsNullOrWhiteSpace(username))
			{
				ShowEditFeedback("Username cannot be empty.", true);
				return;
			}

			if (!string.IsNullOrWhiteSpace(phone) && !Regex.IsMatch(phone, @"^\+?[\d\s\-().]{6,20}$"))
			{
				ShowEditFeedback("Please enter a valid phone number.", true);
				return;
			}

			try
			{
				_profile.FirstName = firstName;
				_profile.LastName = lastName;
				_profile.PhoneNumber = phone;
				_profile.PreferredCurrency = Account.ExtractCurrencyCode(currency);
                _profile.NotifGoalReminders = ChkGoalReminders.IsChecked == true;
                _profile.Save();

				if (username != _currentUser.Username)
				{
					db.UpdateUsername(_currentUser.UserID, username);
					_currentUser.Username = username;
				}

				ShowEditFeedback("Profile saved successfully!", false);
				RefreshDisplayPanel();
			}
			catch (Exception ex)
			{
				ShowEditFeedback("Error saving profile: " + ex.Message, true);
			}
		}

		private void SavePassword_Click(object sender, RoutedEventArgs e)
		{
			string current = CurrentPass.Password;
			string newPw = NewPass.Password;
			string confirm = ConfirmNewPass.Password;

			if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(newPw))
			{
				ShowPassFeedback("Please fill in all password fields.", true);
				return;
			}

			if (!db.ValidateLogin(_currentUser.Email, current))
			{
				ShowPassFeedback("Current password is incorrect.", true);
				return;
			}

			if (!IsPasswordValid(newPw))
			{
				ShowPassFeedback("Password needs 8+ chars, uppercase, number & special character.", true);
				return;
			}

			if (newPw != confirm)
			{
				ShowPassFeedback("Passwords do not match.", true);
				return;
			}

			if (newPw == current)
			{
				ShowPassFeedback("New password can't be the same as your old one.", true);
				return;
			}

			if (ContainsIdentity(newPw, _currentUser.Username, _profile.FirstName, _profile.LastName))
			{
				ShowPassFeedback("Password can't contain your username or name.", true);
				return;
			}

			try
			{
				string hashed = BCrypt.Net.BCrypt.HashPassword(newPw);
				db.UpdateUserPassword(_currentUser.UserID, hashed);
				ShowPassFeedback("Password updated successfully!", false);

				CurrentPass.Clear();
				NewPass.Clear();
				ConfirmNewPass.Clear();
				PassStrengthBar.Value = 0;
				PassStrengthLabel.Text = "";
			}
			catch (Exception ex)
			{
				ShowPassFeedback("Error updating password: " + ex.Message, true);
			}
		}

		private void SaveApiKey_Click(object sender, RoutedEventArgs e)
		{
			string key = ApiKeyInput.Text.Trim();

			if (string.IsNullOrEmpty(key))
			{
				MessageBox.Show("Please paste your OpenAI API key first.", "AI Settings");
				return;
			}

			try
			{
				OpenAiService.SaveApiKey(key);
				MessageBox.Show("API key saved! AI features are now active.", "AI Settings");
			}
			catch (Exception ex)
			{
				MessageBox.Show("Could not save key: " + ex.Message);
			}
		}

		private void ExportData_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				UserReport report = new UserReport(_currentUser.UserID, _currentUser.Username);
				report.GenerateExcel(DateTime.Now.Month, DateTime.Now.Year);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Export failed: " + ex.Message);
			}
		}

        private void ClearAllData_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult first = MessageBox.Show(
                "Are you sure you want to clear all your transaction data?\n\nThis will permanently delete all your transactions and cannot be undone.",
                "Clear All Data",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (first != MessageBoxResult.Yes) return;

            MessageBoxResult second = MessageBox.Show(
                "⚠️ FINAL WARNING\n\nYou are about to permanently delete ALL your transactions.\n\nThis action is irreversible. Are you absolutely sure?",
                "Confirm Permanent Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Stop);

            if (second != MessageBoxResult.Yes) return;

            try
            {
                List<Account> accounts = db.GetAccountsByUser(_currentUser.UserID);
                foreach (Account acc in accounts)
                {
                    List<Transaction> transactions = db.GetTransactionsByAccount(acc.AccountID);
                    foreach (Transaction t in transactions)
                    {
                        db.DeleteTransaction(t.TransactionID);
                    }
                }
                MessageBox.Show("All transaction data has been cleared successfully.",
                    "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error clearing data: " + ex.Message);
            }
        }

        private void AvatarButton_Click(object sender, RoutedEventArgs e)
		{
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
				string destDir = System.IO.Path.Combine(appData, "FinancyApplication", "Avatars");
				System.IO.Directory.CreateDirectory(destDir);
				string destPath = System.IO.Path.Combine(destDir, "avatar_" + _currentUser.UserID + ".jpg");

				BitmapImage bmp = new BitmapImage(new Uri(dlg.FileName, UriKind.Absolute));
				JpegBitmapEncoder encoder = new JpegBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(bmp));
				using (System.IO.FileStream fs = System.IO.File.OpenWrite(destPath))
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
			{
				return;
			}

			Image image = FindVisualChild<Image>(AvatarButton, "AvatarImage");
			Border circle = FindVisualChild<Border>(AvatarButton, "AvatarCircle");

			if (image == null || circle == null)
			{
				return;
			}

			BitmapImage bmp = new BitmapImage();
			bmp.BeginInit();
			bmp.UriSource = new Uri(path, UriKind.Absolute);
			bmp.CacheOption = BitmapCacheOption.OnLoad;
			bmp.EndInit();

			image.Source = bmp;
			image.Visibility = Visibility.Visible;
			circle.Visibility = Visibility.Collapsed;
		}

		// Password strength

		private void NewPass_PasswordChanged(object sender, RoutedEventArgs e)
		{
			UpdatePassStrengthUI(NewPass.Password);
		}

		private int GetPasswordScore(string password)
		{
			if (string.IsNullOrEmpty(password))
			{
				return 0;
			}
			int score = 0;
			if (password.Length >= 8) score++;
			if (password.Length >= 12) score++;
			if (Regex.IsMatch(password, @"[A-Z]") && Regex.IsMatch(password, @"[a-z]")) score++;
			if (Regex.IsMatch(password, @"\d")) score++;
			if (Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]")) score++;
			return Math.Min(score, 4);
		}

		private string GetPasswordLabel(int score)
		{
			if (score <= 1) return "Weak — add more characters";
			if (score == 2) return "Fair — add uppercase & numbers";
			if (score == 3) return "Good — add a special character";
			return "Strong password";
		}

		private Color GetPasswordColor(int score)
		{
			if (score <= 1) return Color.FromRgb(239, 68, 68);
			if (score == 2) return Color.FromRgb(245, 158, 11);
			if (score == 3) return Color.FromRgb(59, 130, 246);
			return Color.FromRgb(0, 184, 148);
		}

		private bool IsPasswordValid(string password)
		{
			if (password.Length < 8) return false;
			if (!Regex.IsMatch(password, @"[A-Z]")) return false;
			if (!Regex.IsMatch(password, @"[a-z]")) return false;
			if (!Regex.IsMatch(password, @"\d")) return false;
			if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]")) return false;
			return true;
		}

		private static bool ContainsIdentity(string password, params string[] tokens)
		{
			if (string.IsNullOrEmpty(password))
			{
				return false;
			}
			string lower = password.ToLowerInvariant();
			foreach (string t in tokens)
			{
				if (string.IsNullOrWhiteSpace(t))
				{
					continue;
				}
				string tl = t.Trim().ToLowerInvariant();
				if (tl.Length >= 3 && lower.Contains(tl))
				{
					return true;
				}
			}
			return false;
		}

		private void UpdatePassStrengthUI(string password)
		{
			int score = GetPasswordScore(password);
			string label = GetPasswordLabel(score);
			Color color = GetPasswordColor(score);

			PassStrengthBar.Value = score;
			PassStrengthLabel.Text = label;
			PassStrengthLabel.Foreground = new SolidColorBrush(color);

			System.Windows.Controls.Border indicator = FindVisualChild<System.Windows.Controls.Border>(PassStrengthBar, "PART_Indicator");
			if (indicator != null)
			{
				indicator.Background = new SolidColorBrush(color);
			}
		}

		private void ShowEditFeedback(string msg, bool isError)
		{
			EditFeedback.Text = msg;
			if (isError)
			{
				EditFeedback.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
			}
			else
			{
				EditFeedback.Foreground = new SolidColorBrush(Color.FromRgb(5, 150, 105));
			}
			EditFeedback.Visibility = Visibility.Visible;
		}

		private void ShowPassFeedback(string msg, bool isError)
		{
			PassFeedback.Text = msg;
			if (isError)
			{
				PassFeedback.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
			}
			else
			{
				PassFeedback.Foreground = new SolidColorBrush(Color.FromRgb(5, 150, 105));
			}
			PassFeedback.Visibility = Visibility.Visible;
		}

		// Walks the WPF visual tree to find a named element inside a ControlTemplate.
		// Needed because elements inside ControlTemplate are not accessible by x:Name directly.
		private static T FindVisualChild<T>(DependencyObject parent, string name)
			where T : FrameworkElement
		{
			if (parent == null)
			{
				return null;
			}
			int count = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < count; i++)
			{
				DependencyObject child = VisualTreeHelper.GetChild(parent, i);
				T castChild = child as T;
				if (castChild != null && castChild.Name == name)
				{
					return castChild;
				}
				T result = FindVisualChild<T>(child, name);
				if (result != null)
				{
					return result;
				}
			}
			return null;
		}

		private void NotifCheckbox_Changed(object sender, RoutedEventArgs e)
{
    if (_profile == null) return;
    try
    {
        _profile.NotifGoalReminders = ChkGoalReminders.IsChecked == true;
        _profile.Save();
    }
    catch { }
}
	}
}
