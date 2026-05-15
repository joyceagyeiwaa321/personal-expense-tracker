using System;
using System.Windows;

namespace FinancyApplication
{
	public partial class AdminProfileWindow : Window
	{
		private readonly Data db = new Data();
		private User _admin;
		private UserProfile _profile;

		public AdminProfileWindow(User admin)
		{
			InitializeComponent();
			_admin = admin;
			Loaded += AdminProfileWindow_Loaded;
		}

		private void AdminProfileWindow_Loaded(object sender, RoutedEventArgs e)
		{
			LoadProfileData();
		}

		private void LoadProfileData()
		{
			try
			{
				// Load admin user info
				UsernameDisplay.Text = $"Username: {_admin.Username}";
				EmailDisplay.Text = _admin.Email;
				RoleDisplay.Text = _admin.Role.ToString();

				// Load profile data
				_profile = db.GetProfileByUserId(_admin.UserID);

				if (_profile != null)
				{
					FirstNameInput.Text = _profile.FirstName ?? "";
					LastNameInput.Text = _profile.LastName ?? "";
					PhoneInput.Text = _profile.PhoneNumber ?? "";
					AvatarUrlInput.Text = _profile.AvatarUrl ?? "";
				}
				else
				{
					// Create empty profile if it doesn't exist
					_profile = new UserProfile
					{
						UserID = _admin.UserID,
						FirstName = "",
						LastName = "",
						PhoneNumber = "",
						AvatarUrl = "",
						PreferredCurrency = "USD"
					};
				}

				UpdateAvatarDisplay();
			}
			catch (Exception ex)
			{
				ShowError("Error loading profile: " + ex.Message);
			}
		}

		private void UpdateAvatarDisplay()
		{
			string initial = _admin.Username.Length > 0 ? _admin.Username.Substring(0, 1).ToUpper() : "A";
			AvatarInitial.Text = initial;
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
			this.Close();
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			// Validate inputs
			string firstName = FirstNameInput.Text?.Trim() ?? "";
			string lastName = LastNameInput.Text?.Trim() ?? "";
			string phone = PhoneInput.Text?.Trim() ?? "";
			string avatarUrl = AvatarUrlInput.Text?.Trim() ?? "";

			// Update profile object
			_profile.FirstName = firstName;
			_profile.LastName = lastName;
			_profile.PhoneNumber = phone;
			_profile.AvatarUrl = avatarUrl;

			try
			{
				// Check if profile exists in DB
				UserProfile existingProfile = db.GetProfileByUserId(_admin.UserID);

				if (existingProfile != null)
				{
					// Update existing profile
					db.UpdateProfile(_profile);
					MessageBox.Show("Profile updated successfully!",
						"Success", MessageBoxButton.OK, MessageBoxImage.Information);
				}
				else
				{
					// Insert new profile if it doesn't exist
					db.InsertProfile(_profile);
					MessageBox.Show("Profile created successfully!",
						"Success", MessageBoxButton.OK, MessageBoxImage.Information);
				}

				this.DialogResult = true;
				this.Close();
			}
			catch (Exception ex)
			{
				ShowError("Error saving profile: " + ex.Message);
			}
		}

		private void ShowError(string message)
		{
			ErrorMessage.Text = message;
			ErrorMessage.Visibility = Visibility.Visible;
		}
	}
}
