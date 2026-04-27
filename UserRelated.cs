using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using BCrypt.Net;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
		public bool IsVerified { get; set; } = false;
		public string ResetToken { get; set; }
		public DateTime CreatedAt { get; set; }

		public User() { }

		public User(string username, string email, string password)
		{
			Username = username;
			Email = email;
			CreatedAt = DateTime.Now;

			if (data.GetUserCount() == 0)
				this.Role = UserRole.Admin;
			else
				this.Role = UserRole.User;

			string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
			this.UserID = data.InsertUser(this, hashedPassword);

			string code = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
			data.UpdateResetToken(email, code);
			EmailService emailService = new EmailService();
			emailService.SendVerificationCode(email, username, code);
		}

		public bool Login(string email, string password)
		{
			try
			{
				string storedHash = data.GetPasswordHash(email);

				if (string.IsNullOrEmpty(storedHash)) return false;

				bool isValid = BCrypt.Net.BCrypt.Verify(password, storedHash);

				if (isValid)
					Application.Current.Properties["CurrentUser"] = email;

				return isValid;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Login error: " + ex.Message);
				return false;
			}
		}

		public void Logout()
		{
			Application.Current.Properties["CurrentUser"] = null;
			MessageBox.Show("You have been logged out successfully.");
		}

		public void ResetPassword(string email, string recipientName)
		{
			try
			{
				string token = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
				data.UpdateResetToken(email, token);

				EmailService emailService = new EmailService();
				bool sent = emailService.SendResetToken(email, recipientName, token);

				if (sent)
					MessageBox.Show("A reset code has been sent to " + email);
				else
					MessageBox.Show("Could not send email. Please try again.");
			}
			catch (Exception ex)
			{
				MessageBox.Show("Reset password error: " + ex.Message);
			}
		}

		public void Deactivate()
		{
			try
			{
				this.IsActive = false;
				data.UpdateUserStatus(this.UserID, false);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Deactivate error: " + ex.Message);
			}
		}

		public void UpdateRole(User callingUser, UserRole newRole)
		{
			if (callingUser.Role != UserRole.Admin)
			{
				MessageBox.Show("Access denied. Only admins can change user roles.");
				return;
			}

			try
			{
				this.Role = newRole;
				data.UpdateUserRole(this.UserID, newRole.ToString());
			}
			catch (Exception ex)
			{
				MessageBox.Show("UpdateRole error: " + ex.Message);
			}
		}
		public bool VerifyAccount(string code)
		{
			try
			{
				string storedToken = data.GetResetToken(this.Email);

				if (string.IsNullOrEmpty(storedToken))
				{
					MessageBox.Show("No verification code found.");
					return false;
				}

				if (storedToken == code)
				{
					this.IsVerified = true;
					data.UpdateVerificationStatus(this.UserID, true);
					data.UpdateResetToken(this.Email, "");
					MessageBox.Show("Account verified successfully!");
					return true;
				}
				else
				{
					MessageBox.Show("Invalid verification code.");
					return false;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("VerifyAccount error: " + ex.Message);
				return false;
			}
		}

		public override string ToString() => $"{Username} ({Role})";
	}

	public class Admin : User
	{
		public Admin() : base() { this.Role = UserRole.Admin; }

		public List<User> GetAllUsers()
		{
			try
			{
				return data.GetAllUsers();
			}
			catch (Exception ex)
			{
				MessageBox.Show("GetAllUsers error: " + ex.Message);
				return new List<User>();
			}
		}

		public List<Transaction> ViewAllTransactions()
		{
			try
			{
				return data.GetAllTransactions();
			}
			catch (Exception ex)
			{
				MessageBox.Show("ViewAllTransactions error: " + ex.Message);
				return new List<Transaction>();
			}
		}

		public void DeactivateUser(int userId)
		{
			try
			{
				data.UpdateUserStatus(userId, false);
			}
			catch (Exception ex)
			{
				MessageBox.Show("DeactivateUser error: " + ex.Message);
			}
		}

		public void DeleteUser(int userId)
		{
			try
			{
				data.DeleteUser(userId);
			}
			catch (Exception ex)
			{
				MessageBox.Show("DeleteUser error: " + ex.Message);
			}
		}

		public void ResetUserPassword(int userId)
		{
			try
			{
				string newPassword = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
				string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
				data.UpdateUserPassword(userId, hashedPassword);
				MessageBox.Show("Password has been reset. New temporary password: " + newPassword);
			}
			catch (Exception ex)
			{
				MessageBox.Show("ResetUserPassword error: " + ex.Message);
			}
		}

		public void ManageDefaultCategories()
		{
			MessageBox.Show("Opening Default Category Management...");
		}

		public void ManageGroups()
		{
			MessageBox.Show("Opening Group Management...");
		}

		public void GenerateReport()
		{
			try
			{
				int totalUsers = data.GetUserCount();
				int activeUsers = data.GetActiveUserCount();
				int inactiveUsers = totalUsers - activeUsers;
				int totalAccounts = data.GetTotalAccountCount();
				int totalTransactions = data.GetTotalTransactionCount();

				string reportContent =
					"================================================" + "\n" +
					"           FINANCY SYSTEM REPORT               " + "\n" +
					"================================================" + "\n" +
					"Generated:         " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" +
					"Generated by:      " + this.Username + "\n" +
					"------------------------------------------------" + "\n" +
					"USER STATISTICS" + "\n" +
					"------------------------------------------------" + "\n" +
					"Total Users:       " + totalUsers + "\n" +
					"Active Users:      " + activeUsers + "\n" +
					"Inactive Users:    " + inactiveUsers + "\n" +
					"------------------------------------------------" + "\n" +
					"SYSTEM STATISTICS" + "\n" +
					"------------------------------------------------" + "\n" +
					"Total Accounts:    " + totalAccounts + "\n" +
					"Total Transactions:" + totalTransactions + "\n" +
					"================================================" + "\n";

				string path = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					"Downloads",
					"FinancyReport_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt"
				);

				File.WriteAllText(path, reportContent);
				MessageBox.Show("Report saved to Downloads: " + path);
			}
			catch (Exception ex)
			{
				MessageBox.Show("GenerateReport error: " + ex.Message);
			}
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

		public void Save()
		{
			try
			{
				data.UpdateProfile(this);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Save profile error: " + ex.Message);
			}
		}

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

	public class UserReport
	{
		private Data data = new Data();
		private int userID;
		private string username;

		public UserReport(int userId, string username)
		{
			this.userID = userId;
			this.username = username;
			QuestPDF.Settings.License = LicenseType.Community;
		}

		private List<Transaction> GetMonthlyTransactions(int month, int year)
		{
			List<Transaction> all = data.GetAllTransactions();
			return all.Where(t => t.UserID == userID && t.Date.Month == month && t.Date.Year == year).ToList();
		}

		public void GeneratePDF(int month, int year)
		{
			try
			{
				List<Transaction> transactions = GetMonthlyTransactions(month, year);
				decimal totalIncome = transactions.Where(t => t.Type == "Income").Sum(t => t.Amount);
				decimal totalExpense = transactions.Where(t => t.Type == "Expense").Sum(t => t.Amount);
				decimal net = totalIncome - totalExpense;

				string monthName = new DateTime(year, month, 1).ToString("MMMM yyyy");
				string path = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					"Downloads",
					$"FinancyReport_{username}_{year}_{month:00}.pdf"
				);

				Document.Create(container =>
				{
					container.Page(page =>
					{
						page.Size(PageSizes.A4);
						page.Margin(40);
						page.DefaultTextStyle(x => x.FontSize(11));

						page.Content().Column(col =>
						{
							col.Item().Text("Financy — Monthly Report")
								.FontSize(22).Bold().FontColor("#2e7d32");
							col.Item().Text($"User: {username}").FontSize(13);
							col.Item().Text($"Period: {monthName}").FontSize(13);
							col.Item().PaddingVertical(10).LineHorizontal(1).LineColor("#cccccc");

							col.Item().Text("Summary").FontSize(15).Bold();
							col.Item().Text($"Total Income:   {totalIncome:F2}");
							col.Item().Text($"Total Expenses: {totalExpense:F2}");
							col.Item().Text($"Net Balance:    {net:F2}");
							col.Item().PaddingVertical(10).LineHorizontal(1).LineColor("#cccccc");

							col.Item().Text("Transactions").FontSize(15).Bold();
							col.Item().PaddingTop(5).Table(table =>
							{
								table.ColumnsDefinition(columns =>
								{
									columns.RelativeColumn(2);
									columns.RelativeColumn(2);
									columns.RelativeColumn(1);
									columns.RelativeColumn(2);
								});

								table.Header(header =>
								{
									header.Cell().Background("#2e7d32").Padding(5)
										.Text("Date").FontColor("#ffffff").Bold();
									header.Cell().Background("#2e7d32").Padding(5)
										.Text("Description").FontColor("#ffffff").Bold();
									header.Cell().Background("#2e7d32").Padding(5)
										.Text("Type").FontColor("#ffffff").Bold();
									header.Cell().Background("#2e7d32").Padding(5)
										.Text("Amount").FontColor("#ffffff").Bold();
								});

								foreach (Transaction t in transactions)
								{
									string bg = t.Type == "Income" ? "#e8f5e9" : "#ffebee";
									table.Cell().Background(bg).Padding(5).Text(t.Date.ToString("yyyy-MM-dd"));
									table.Cell().Background(bg).Padding(5).Text(t.Description);
									table.Cell().Background(bg).Padding(5).Text(t.Type);
									table.Cell().Background(bg).Padding(5).Text(t.Amount.ToString("F2"));
								}
							});
						});
					});
				}).GeneratePdf(path);

				MessageBox.Show("PDF report saved to Downloads:\n" + path);
			}
			catch (Exception ex)
			{
				MessageBox.Show("GeneratePDF error: " + ex.Message);
			}
		}

		public void GenerateExcel(int month, int year)
		{
			try
			{
				List<Transaction> transactions = GetMonthlyTransactions(month, year);
				decimal totalIncome = transactions.Where(t => t.Type == "Income").Sum(t => t.Amount);
				decimal totalExpense = transactions.Where(t => t.Type == "Expense").Sum(t => t.Amount);
				decimal net = totalIncome - totalExpense;

				string path = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					"Downloads",
					$"FinancyReport_{username}_{year}_{month:00}.xlsx"
				);

				using (XLWorkbook workbook = new XLWorkbook())
				{
					IXLWorksheet ws = workbook.Worksheets.Add("Monthly Report");

					ws.Cell("A1").Value = "Financy Monthly Report";
					ws.Cell("A1").Style.Font.Bold = true;
					ws.Cell("A1").Style.Font.FontSize = 16;
					ws.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#2e7d32");

					ws.Cell("A2").Value = $"User: {username}";
					ws.Cell("A3").Value = $"Period: {new DateTime(year, month, 1):MMMM yyyy}";

					ws.Cell("A5").Value = "Summary";
					ws.Cell("A5").Style.Font.Bold = true;
					ws.Cell("A6").Value = "Total Income";
					ws.Cell("B6").Value = totalIncome;
					ws.Cell("A7").Value = "Total Expenses";
					ws.Cell("B7").Value = totalExpense;
					ws.Cell("A8").Value = "Net Balance";
					ws.Cell("B8").Value = net;
					ws.Cell("B8").Style.Font.Bold = true;

					ws.Cell("A10").Value = "Date";
					ws.Cell("B10").Value = "Description";
					ws.Cell("C10").Value = "Type";
					ws.Cell("D10").Value = "Amount";

					IXLRange headerRange = ws.Range("A10:D10");
					headerRange.Style.Font.Bold = true;
					headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2e7d32");
					headerRange.Style.Font.FontColor = XLColor.White;

					int row = 11;
					foreach (Transaction t in transactions)
					{
						ws.Cell(row, 1).Value = t.Date.ToString("yyyy-MM-dd");
						ws.Cell(row, 2).Value = t.Description;
						ws.Cell(row, 3).Value = t.Type;
						ws.Cell(row, 4).Value = t.Amount;

						string bg = t.Type == "Income" ? "#e8f5e9" : "#ffebee";
						ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.FromHtml(bg);
						row++;
					}

					ws.Columns().AdjustToContents();
					workbook.SaveAs(path);
				}

				MessageBox.Show("Excel report saved to Downloads:\n" + path);
			}
			catch (Exception ex)
			{
				MessageBox.Show("GenerateExcel error: " + ex.Message);
			}
		}
	}
}