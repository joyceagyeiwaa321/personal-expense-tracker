using System;
using System.Windows;

namespace FinancyApplication
{
	public partial class CreateUserWindow : Window
	{
		private readonly Data db = new Data();
		public bool UserCreated { get; set; } = false;

		public CreateUserWindow()
		{
			InitializeComponent();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
			this.Close();
		}

		private void Create_Click(object sender, RoutedEventArgs e)
		{
			// Validate inputs
			string username = UsernameInput.Text?.Trim();
			string email = EmailInput.Text?.Trim();
			string password = PasswordInput.Password;
			string confirmPassword = ConfirmPasswordInput.Password;
			string role = (RoleComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "User";

			ErrorMessage.Visibility = Visibility.Collapsed;
			ErrorMessage.Text = "";

			// Validation
			if (string.IsNullOrWhiteSpace(username))
			{
				ShowError("Username is required.");
				return;
			}

			if (username.Length < 3)
			{
				ShowError("Username must be at least 3 characters long.");
				return;
			}

			if (string.IsNullOrWhiteSpace(email))
			{
				ShowError("Email is required.");
				return;
			}

			if (!email.Contains("@"))
			{
				ShowError("Please enter a valid email address.");
				return;
			}

			// Check if email already exists
			try
			{
				if (db.EmailExists(email))
				{
					ShowError("This email address is already registered.");
					return;
				}
			}
			catch (Exception ex)
			{
				ShowError("Error checking email: " + ex.Message);
				return;
			}

			if (string.IsNullOrWhiteSpace(password))
			{
				ShowError("Password is required.");
				return;
			}

			if (password.Length < 6)
			{
				ShowError("Password must be at least 6 characters long.");
				return;
			}

			if (password != confirmPassword)
			{
				ShowError("Passwords do not match.");
				return;
			}

			// Hash the password
			string hashedPassword;
			try
			{
				hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
			}
			catch (Exception ex)
			{
				ShowError("Error hashing password: " + ex.Message);
				return;
			}

			// Create user
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

				int newUserId = db.InsertUser(newUser, hashedPassword);

				MessageBox.Show(
					$"User '{username}' has been created successfully.",
					"Success",
					MessageBoxButton.OK,
					MessageBoxImage.Information);

				UserCreated = true;
				this.DialogResult = true;
				this.Close();
			}
			catch (Exception ex)
			{
				ShowError("Error creating user: " + ex.Message);
			}
		}

		private void ShowError(string message)
		{
			ErrorMessage.Text = message;
			ErrorMessage.Visibility = Visibility.Visible;
		}
	}
}
