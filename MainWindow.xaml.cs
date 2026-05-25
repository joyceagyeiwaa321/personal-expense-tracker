using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace FinancyApplication
{
	public partial class MainWindow : Window
	{
		private DispatcherTimer resendTimer;
		private DispatcherTimer regResendTimer;
		private int secondsRemaining = 30;
		private int regSecondsRemaining = 30;
		private Data db = new Data();

		// In-memory expiry for password reset codes — email -> issue time (UTC). Survives only the app session.
		private static readonly Dictionary<string, DateTime> _resetTokenIssuedAt = new Dictionary<string, DateTime>();
		private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromMinutes(15);

		// Track show/hide state for each password field
		private bool loginPassVisible = false;
		private bool regPassVisible = false;
		private bool regConfirmPassVisible = false;
		private bool resetNewPassVisible = false;
		private bool resetRepeatPassVisible = false;

		public MainWindow()
		{
			InitializeComponent();
			SetupTimers();
		}

		// ── WINDOW CONTROLS ──────────────────────────────────────────────
		private void Minimize_Click(object sender, RoutedEventArgs e) =>
			this.WindowState = WindowState.Minimized;

		private void Maximize_Click(object sender, RoutedEventArgs e) =>
			this.WindowState = (this.WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;

		private void Exit_Click(object sender, RoutedEventArgs e) =>
			Application.Current.Shutdown();

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left) this.DragMove();
		}

		// ── SHOW / HIDE PASSWORD TOGGLES ─────────────────────────────────
		private void LoginTogglePass_Click(object sender, RoutedEventArgs e)
		{
			loginPassVisible = !loginPassVisible;
			if (loginPassVisible)
			{
				PasswordInputVisible.Text = PasswordInput.Password;
				PasswordInput.Visibility = Visibility.Collapsed;
				PasswordInputVisible.Visibility = Visibility.Visible;
			}
			else
			{
				PasswordInput.Password = PasswordInputVisible.Text;
				PasswordInputVisible.Visibility = Visibility.Collapsed;
				PasswordInput.Visibility = Visibility.Visible;
			}
		}

		private void RegTogglePass_Click(object sender, RoutedEventArgs e)
		{
			regPassVisible = !regPassVisible;
			if (regPassVisible)
			{
				RegPassVisible.Text = RegPass.Password;
				RegPass.Visibility = Visibility.Collapsed;
				RegPassVisible.Visibility = Visibility.Visible;
			}
			else
			{
				RegPass.Password = RegPassVisible.Text;
				RegPassVisible.Visibility = Visibility.Collapsed;
				RegPass.Visibility = Visibility.Visible;
			}
		}

		private void RegToggleConfirmPass_Click(object sender, RoutedEventArgs e)
		{
			regConfirmPassVisible = !regConfirmPassVisible;
			if (regConfirmPassVisible)
			{
				RegConfirmPassVisible.Text = RegConfirmPass.Password;
				RegConfirmPass.Visibility = Visibility.Collapsed;
				RegConfirmPassVisible.Visibility = Visibility.Visible;
			}
			else
			{
				RegConfirmPass.Password = RegConfirmPassVisible.Text;
				RegConfirmPassVisible.Visibility = Visibility.Collapsed;
				RegConfirmPass.Visibility = Visibility.Visible;
			}
		}

		private void ResetTogglePass_Click(object sender, RoutedEventArgs e)
		{
			resetNewPassVisible = !resetNewPassVisible;
			if (resetNewPassVisible)
			{
				NewPassVisible.Text = NewPass.Password;
				NewPass.Visibility = Visibility.Collapsed;
				NewPassVisible.Visibility = Visibility.Visible;
			}
			else
			{
				NewPass.Password = NewPassVisible.Text;
				NewPassVisible.Visibility = Visibility.Collapsed;
				NewPass.Visibility = Visibility.Visible;
			}
		}

		private void ResetToggleConfirmPass_Click(object sender, RoutedEventArgs e)
		{
			resetRepeatPassVisible = !resetRepeatPassVisible;
			if (resetRepeatPassVisible)
			{
				RepeatPassVisible.Text = RepeatPass.Password;
				RepeatPass.Visibility = Visibility.Collapsed;
				RepeatPassVisible.Visibility = Visibility.Visible;
			}
			else
			{
				RepeatPass.Password = RepeatPassVisible.Text;
				RepeatPassVisible.Visibility = Visibility.Collapsed;
				RepeatPass.Visibility = Visibility.Visible;
			}
		}

		// ── PASSWORD STRENGTH ─────────────────────────────────────────────
		// Industry standard: 8+ chars, uppercase, lowercase, number, special char
		private (int score, string label, Color color) GetPasswordStrength(string password)
		{
			if (string.IsNullOrEmpty(password)) return (0, "", Colors.Transparent);

			int score = 0;
			if (password.Length >= 8) score++;
			if (password.Length >= 12) score++;
			if (Regex.IsMatch(password, @"[A-Z]") && Regex.IsMatch(password, @"[a-z]")) score++;
			if (Regex.IsMatch(password, @"\d")) score++;
			if (Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]")) score++;

			// Cap at 4 for the progress bar
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
			// Minimum requirements: 8 chars, 1 uppercase, 1 lowercase, 1 number, 1 special
			return password.Length >= 8
				&& Regex.IsMatch(password, @"[A-Z]")
				&& Regex.IsMatch(password, @"[a-z]")
				&& Regex.IsMatch(password, @"\d")
				&& Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.
	<>\/?]");
		}

		// Rejects passwords that embed the user's identity (username / real name).
		// Tokens shorter than 3 chars are ignored so a one-letter name doesn't block everything.
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

		private void UpdateStrengthUI(string password)
		{
			var (score, label, color) = GetPasswordStrength(password);
			StrengthBar.Value = score;
			StrengthLabel.Text = label;

			// Update the bar fill colour
			var indicator = FindChild
		<System.Windows.Controls.Border>(StrengthBar, "PART_Indicator");
			if (indicator != null)
				indicator.Background = new SolidColorBrush(color);

			StrengthLabel.Foreground = new SolidColorBrush(color);
		}

		// Helper to find named child in template
		private static T FindChild
			<T>(DependencyObject parent, string name) where T : FrameworkElement
		{
			if (parent == null) return null;
			int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i
				< count; i++)
			{
				var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
				if (child is T t && t.Name == name) return t;
				var result = FindChild<T>(child, name);
				if (result != null) return result;
			}
			return null;
		}

		private void RegPass_PasswordChanged(object sender, RoutedEventArgs e) =>
			UpdateStrengthUI(RegPass.Password);

		private void RegPassVisible_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) =>
			UpdateStrengthUI(RegPassVisible.Text);

		// ── LOGIN ─────────────────────────────────────────────────────────
		private void Login_Click(object sender, RoutedEventArgs e)
		{
			string email = EmailInput.Text.Trim();
			string password = loginPassVisible ? PasswordInputVisible.Text : PasswordInput.Password;

			if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
			{
				ShowNotification("Please fill in all fields.", true);
				return;
			}

			if (db.ValidateLogin(email, password))
			{
				var user = db.GetUserByEmail(email);
				Application.Current.Properties["CurrentUser"] = user;

				// Admins land on the Access Management console instead of the
				// regular Dashboard — they don't need the personal finance views.
				if (user.Role == UserRole.Admin)
				{
					new AdminWindow { CurrentUser = user }.Show();
				}
				else
				{
					new MainAppWindow(user).Show();
				}
				this.Close();
			}
			else
			{
				ShowNotification("Invalid email or password.", true);
			}
		}

		// ── REGISTRATION ──────────────────────────────────────────────────
		private void RegisterStep1_Click(object sender, RoutedEventArgs e)
		{
			string email = RegEmail.Text.Trim();
			string pass = regPassVisible ? RegPassVisible.Text : RegPass.Password;
			string confirm = regConfirmPassVisible ? RegConfirmPassVisible.Text : RegConfirmPass.Password;

			if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
			{
				ShowNotification("Please fill in all fields.", true); return;
			}

			if (!IsPasswordValid(pass))
			{
				ShowNotification("Password needs 8+ chars, uppercase, number & special character.", true);
				return;
			}

			if (pass != confirm)
			{
				ShowNotification("Passwords do not match!", true); return;
			}

			if (db.EmailExists(email))
			{
				ShowNotification("Email already registered!", true); return;
			}

			string code = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
			if (new EmailService().SendVerificationCode(email, "New User", code))
			{
				Application.Current.Properties["RegEmail"] = email;
				Application.Current.Properties["RegPass"] = pass;
				Application.Current.Properties["RegCode"] = code;
				HideAllViews();
				RegVerifyView.Visibility = Visibility.Visible;
				StartRegCooldown();
			}
		}

		private void RegisterStep2_Click(object sender, RoutedEventArgs e)
		{
			if (RegCodeInput.Text.Trim().ToUpper() == (Application.Current.Properties["RegCode"] as string))
			{
				HideAllViews();
				RegUsernameView.Visibility = Visibility.Visible;
			}
			else ShowNotification("Invalid verification code.", true);
		}

		private void RegisterStep3_Click(object sender, RoutedEventArgs e)
		{
			string username = RegUsername.Text.Trim();
			if (string.IsNullOrEmpty(username)) { ShowNotification("Please enter a username.", true); return; }

			// Password from step 1 must not contain the chosen username
			string regPass = Application.Current.Properties["RegPass"] as string;
			if (ContainsIdentity(regPass, username))
			{
				ShowNotification("Username can't appear in your password — pick a different one.", true);
				return;
			}

			string hashed = BCrypt.Net.BCrypt.HashPassword(Application.Current.Properties["RegPass"] as string);
			User newUser = new User
			{
				Username = username,
				Email = Application.Current.Properties["RegEmail"] as string,
				Role = UserRole.User,
				CreatedAt = DateTime.Now,
				IsActive = true
			};

			if (db.InsertUser(newUser, hashed) > 0)
			{
				// Auto-login after signup. The first-ever account is silently
				// promoted to Admin in the User constructor, so honour that and
				// route them straight into the Access Management console.
				var user = db.GetUserByEmail(newUser.Email);
				Application.Current.Properties["CurrentUser"] = user;
				if (user.Role == UserRole.Admin)
				{
					new AdminWindow { CurrentUser = user }.Show();
				}
				else
				{
					new MainAppWindow(user).Show();
				}
				this.Close();
			}
			else
			{
				ShowNotification("Could not create account. Try again.", true);
			}
		}

		private void RegisterStep2_Back_Click(object sender, RoutedEventArgs e)
		{
			HideAllViews();
			RegVerifyView.Visibility = Visibility.Visible;
		}

		// ── PASSWORD RESET ────────────────────────────────────────────────
		private void SendReset_Click(object sender, RoutedEventArgs e)
		{
			string email = ResetEmailInput.Text.Trim();
			User user = db.GetUserByEmail(email);
			if (user != null && db.ResendPasswordReset(email, user.Username))
			{
				_resetTokenIssuedAt[email] = DateTime.UtcNow;   // start the 15-min clock
				HideAllViews();
				VerifyView.Visibility = Visibility.Visible;
				StartCooldown();
			}
			else ShowNotification("Email not found.", true);
		}

		private void VerifyCode_Click(object sender, RoutedEventArgs e)
		{
			string email = ResetEmailInput.Text.Trim();

			// Enforce the 15-min lifetime the email promises
			if (!_resetTokenIssuedAt.TryGetValue(email, out DateTime issuedAt)
				|| DateTime.UtcNow - issuedAt > ResetTokenLifetime)
			{
				ShowNotification("This code has expired. Please request a new one.", true);
				return;
			}

			if (CodeInput.Text.Trim() == db.GetResetToken(email))
			{
				_resetTokenIssuedAt.Remove(email);   // single-use
				HideAllViews();
				NewPasswordView.Visibility = Visibility.Visible;
			}
			else ShowNotification("Invalid code.", true);
		}

		private void FinalReset_Click(object sender, RoutedEventArgs e)
		{
			string newPass = resetNewPassVisible ? NewPassVisible.Text : NewPass.Password;
			string repeatPass = resetRepeatPassVisible ? RepeatPassVisible.Text : RepeatPass.Password;
			string email = ResetEmailInput.Text.Trim();

			if (!IsPasswordValid(newPass))
			{
				ShowNotification("Password needs 8+ chars, uppercase, number & special character.", true);
				return;
			}

			if (newPass != repeatPass)
			{
				ShowNotification("Passwords do not match!", true); return;
			}

			User user = db.GetUserByEmail(email);
			if (user == null) { ShowNotification("Account not found.", true); return; }

			// Same rules as registration PLUS can't reuse the old password
			string oldHash = db.GetPasswordHash(email);
			if (!string.IsNullOrEmpty(oldHash) && BCrypt.Net.BCrypt.Verify(newPass, oldHash))
			{
				ShowNotification("New password can't be the same as your old one.", true);
				return;
			}

			// Can't contain username or real name
			UserProfile profile = db.GetProfileByUserId(user.UserID);
			if (ContainsIdentity(newPass, user.Username, profile?.FirstName, profile?.LastName))
			{
				ShowNotification("Password can't contain your username or name.", true);
				return;
			}

			db.UpdateUserPassword(user.UserID, BCrypt.Net.BCrypt.HashPassword(newPass));
			ShowNotification("Password updated successfully!");
			ShowLogin_Click(null, null);
		}

		// ── VIEW NAVIGATION ───────────────────────────────────────────────
		private void HideAllViews()
		{
			LoginView.Visibility = RegisterView.Visibility = RegVerifyView.Visibility =
			RegUsernameView.Visibility = ResetView.Visibility = VerifyView.Visibility =
			NewPasswordView.Visibility = Visibility.Collapsed;
		}

		private void ShowReset_Click(object sender, RoutedEventArgs e) { HideAllViews(); ResetView.Visibility = Visibility.Visible; }
		private void ShowRegister_Click(object sender, RoutedEventArgs e) { HideAllViews(); RegisterView.Visibility = Visibility.Visible; }
		private void ShowLogin_Click(object sender, RoutedEventArgs e) { HideAllViews(); LoginView.Visibility = Visibility.Visible; }

		// ── TIMERS ────────────────────────────────────────────────────────
		private void SetupTimers()
		{
			resendTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
			resendTimer.Tick += (s, e) =>
			{
				secondsRemaining--;
				TimerText.Text = $"Resend available in {secondsRemaining}s";
				if (secondsRemaining
					<= 0) { resendTimer.Stop(); SendCodeBtn.IsEnabled = true; TimerText.Visibility = Visibility.Collapsed; }
			};

			regResendTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
			regResendTimer.Tick += (s, e) =>
			{
				regSecondsRemaining--;
				RegTimerText.Text = $"Resend available in {regSecondsRemaining}s";
				if (regSecondsRemaining
						<= 0) { regResendTimer.Stop(); RegResendBtn.IsEnabled = true; RegTimerText.Visibility = Visibility.Collapsed; }
			};
		}

		private void StartCooldown()
		{
			SendCodeBtn.IsEnabled = false;
			secondsRemaining = 30;
			TimerText.Visibility = Visibility.Visible;
			resendTimer.Start();
		}

		private void StartRegCooldown()
		{
			RegResendBtn.IsEnabled = false;
			regSecondsRemaining = 30;
			RegTimerText.Visibility = Visibility.Visible;
			regResendTimer.Start();
		}

		// ── NOTIFICATIONS ─────────────────────────────────────────────────
		private async void ShowNotification(string message, bool isError = false)
		{
			NotificationText.Text = message;
			NotificationToast.Background = isError
				? new SolidColorBrush(Color.FromRgb(254, 226, 226))
				: new SolidColorBrush(Color.FromRgb(209, 250, 229));
			NotificationText.Foreground = isError
				? new SolidColorBrush(Color.FromRgb(153, 27, 27))
				: new SolidColorBrush(Color.FromRgb(6, 78, 59));
			NotificationToast.Visibility = Visibility.Visible;
			await Task.Delay(3000);
			NotificationToast.Visibility = Visibility.Collapsed;
		}
	}
}
