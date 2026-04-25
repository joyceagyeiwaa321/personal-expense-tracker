using System;
using System.IO;
using System.Windows;
using BCrypt.Net;

namespace FinancyApplication
{
	public enum UserRole { User, Admin }

	public class User
	{
		protected Data data = new Data();
		public int UserID { get; set; }
		public string Username { get; set; }
		public string Email { get; set; }
		public UserRole Role { get; set; }
		public bool IsActive { get; set; } = true;
		public string ResetToken { get; set; }
		public DateTime CreatedAt { get; set; }

		public User() { }

		public User(string username, string email, string password)
		{
			Username = username;
			Email = email;
			CreatedAt = DateTime.Now;

			// First user ever registered becomes Admin automatically
			if (data.GetUserCount() == 0)
				this.Role = UserRole.Admin;
			else
				this.Role = UserRole.User;

			string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
			this.UserID = data.InsertUser(this, hashedPassword);
		}

		public bool Login(string email, string password)
		{
			// Fetch the stored BCrypt hash from the DB
			string storedHash = data.GetPasswordHash(email);

			if (string.IsNullOrEmpty(storedHash)) return false;

			// Verify the typed password against the stored hash
			bool isValid = BCrypt.Net.BCrypt.Verify(password, storedHash);

			if (isValid)
				Application.Current.Properties["CurrentUser"] = email;

			return isValid;
		}

		public void Logout()
		{
			Application.Current.Properties["CurrentUser"] = null;
			MessageBox.Show("You have been logged out successfully.");
		}

		public void ResetPassword(string email)
		{
			string token = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
			data.UpdateResetToken(email, token);
			MessageBox.Show($"A reset token has been generated: {token}");
		}

		public void Deactivate()
		{
			this.IsActive = false;
			data.UpdateUserStatus(this.UserID, false);
		}

		// Only an Admin is allowed to change another user's role
		public void UpdateRole(User callingUser, UserRole newRole)
		{
			if (callingUser.Role != UserRole.Admin)
			{
				MessageBox.Show("Access denied. Only admins can change user roles.");
				return;
			}

			this.Role = newRole;
			data.UpdateUserRole(this.UserID, newRole.ToString());
		}

		public override string ToString() => $"{Username} ({Role})";
	}

	public class Admin : User
	{
		public Admin() : base() { this.Role = UserRole.Admin; }

		public void DeleteUser(int id) => data.DeleteUser(id);

		public void ManageCategories()
		{
			MessageBox.Show("Opening Category Management Dashboard...");
		}

		public void GenerateReport()
		{
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "FinancyReport.txt");
			string reportContent = $"FINANCY SYSTEM REPORT\nGenerated: {DateTime.Now}\nAdmin: {this.Username}";

			File.WriteAllText(path, reportContent);
			MessageBox.Show($"Report successfully saved to your Desktop: {path}");
		}
	}

	public class UserProfile
	{
		private Data data = new Data();
		public int ProfileID { get; set; }
		public int UserID { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string PhoneNumber { get; set; }
		public string AvatarUrl { get; set; }
		public string PreferredCurrency { get; set; }

		public void Save() => data.UpdateProfile(this);
		public string GetFullName() => $"{FirstName} {LastName}";

		public void UploadAvatar(string path)
		{
			if (File.Exists(path))
			{
				this.AvatarUrl = path;
				this.Save();
			}
			else
			{
				MessageBox.Show("The image file was not found.");
			}
		}
	}
}