using System;
using System.Windows;
using System.Windows.Input;
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

		public MainWindow()
		{
			InitializeComponent();
			SetupTimers();
		}

		private void Maximize_Click(object sender, RoutedEventArgs e) =>
			this.WindowState = (this.WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;

		private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) this.DragMove(); }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (db.ValidateLogin(EmailInput.Text.Trim(), PasswordInput.Password))
            {
                var user = db.GetUserByEmail(EmailInput.Text.Trim());
                Application.Current.Properties["CurrentUser"] = user;
                new Dashboard { CurrentUser = user }.Show();
                this.Close();
            }
            else
                ShowNotification("Invalid email or password.", true);
        }

        private void RegisterStep1_Click(object sender, RoutedEventArgs e)
		{
			string email = RegEmail.Text.Trim();
			string pass = RegPass.Password;

			if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass)) { ShowNotification("Fill all fields.", true); return; }
			if (db.EmailExists(email)) { ShowNotification("Email already registered!", true); return; }

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
			else ShowNotification("Invalid code.", true);
		}

		private void RegisterStep3_Click(object sender, RoutedEventArgs e)
		{
			string user = RegUsername.Text.Trim();
			if (string.IsNullOrEmpty(user)) return;

			string hashed = BCrypt.Net.BCrypt.HashPassword(Application.Current.Properties["RegPass"] as string); 
            User newUser = new User { Username = user, Email = Application.Current.Properties["RegEmail"] as string, Role = UserRole.User, CreatedAt = DateTime.Now, IsActive = true };

			if (db.InsertUser(newUser, hashed) > 0)
            {
				ShowNotification("Account created!");
				ShowLogin_Click(null, null);
			}
		}

		private void RegisterStep2_Back_Click(object sender, RoutedEventArgs e) { HideAllViews(); RegVerifyView.Visibility = Visibility.Visible; }

		private void SendReset_Click(object sender, RoutedEventArgs e)
		{
			string email = ResetEmailInput.Text.Trim();
			User user = db.GetUserByEmail(email);
            if (user != null && db.ResendPasswordReset(email, user.Username)) 
            {
				HideAllViews();
				VerifyView.Visibility = Visibility.Visible;
				StartCooldown();
			}
		}

		private void VerifyCode_Click(object sender, RoutedEventArgs e)
		{
			if (CodeInput.Text.Trim() == db.GetResetToken(ResetEmailInput.Text.Trim())) 
            {
				HideAllViews();
				NewPasswordView.Visibility = Visibility.Visible;
			}
		}

		private void FinalReset_Click(object sender, RoutedEventArgs e)
		{
			if (NewPass.Password != RepeatPass.Password) return;
			User user = db.GetUserByEmail(ResetEmailInput.Text.Trim()); 
            if (user != null)
			{
				db.UpdateUserPassword(user.UserID, BCrypt.Net.BCrypt.HashPassword(NewPass.Password)); 
                ShowNotification("Password updated!");
				ShowLogin_Click(null, null);
			}
		}

		private void HideAllViews() { LoginView.Visibility = RegisterView.Visibility = RegVerifyView.Visibility = RegUsernameView.Visibility = ResetView.Visibility = VerifyView.Visibility = NewPasswordView.Visibility = Visibility.Collapsed; }
		private void ShowReset_Click(object sender, RoutedEventArgs e) { HideAllViews(); ResetView.Visibility = Visibility.Visible; }
		private void ShowRegister_Click(object sender, RoutedEventArgs e) { HideAllViews(); RegisterView.Visibility = Visibility.Visible; }
		private void ShowLogin_Click(object sender, RoutedEventArgs e) { HideAllViews(); LoginView.Visibility = Visibility.Visible; }

		private void SetupTimers()
		{
			resendTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
			resendTimer.Tick += (s, e) => { secondsRemaining--; TimerText.Text = $"Wait {secondsRemaining}s"; if (secondsRemaining <= 0) { resendTimer.Stop(); SendCodeBtn.IsEnabled = true; } };
			regResendTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
			regResendTimer.Tick += (s, e) => { regSecondsRemaining--; RegTimerText.Text = $"Wait {regSecondsRemaining}s"; if (regSecondsRemaining <= 0) { regResendTimer.Stop(); RegResendBtn.IsEnabled = true; } };
		}

		private void StartCooldown() { SendCodeBtn.IsEnabled = false; secondsRemaining = 30; resendTimer.Start(); }
		private void StartRegCooldown() { RegResendBtn.IsEnabled = false; regSecondsRemaining = 30; regResendTimer.Start(); }

		private async void ShowNotification(string message, bool isError = false)
		{
			NotificationText.Text = message;
			NotificationToast.Background = isError ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 235, 238)) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 245, 233));
			NotificationToast.Visibility = Visibility.Visible;
			await Task.Delay(3000);
			NotificationToast.Visibility = Visibility.Collapsed;
		}
	}
}