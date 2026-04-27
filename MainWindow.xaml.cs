using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace FinancyApplication
{
	public partial class MainWindow : Window
	{
		private StringBuilder log = new StringBuilder();

		public MainWindow()
		{
			InitializeComponent();
		}

		private void Log(string message)
		{
			log.AppendLine(message);
			OutputBox.Text = log.ToString();
		}
			log.AppendLine("===========================================");
			log.AppendLine("       FINANCY - FULL SYSTEM TEST          ");
			log.AppendLine("===========================================\n");

			try
			{
				int userId = Test_UserRegistration();
				int profileId = Test_UserProfile(userId);
				Test_Login_Logout(userId);
				Test_ResetPassword();
				Test_UpdateRole(userId);
				Test_DeactivateUser(userId);
				int accountId = Test_Account(userId);
				int categoryId = Test_Category(userId);
				int transId = Test_Transaction(userId, accountId, categoryId);
				Test_AttachReceipt(transId);
				Test_Categorize(transId, categoryId);
				Test_GetTransactionHistory(accountId);
				Test_GetTransactionsByCategory(categoryId);
				Test_AdminMethods(userId);
				Test_Budget(userId, categoryId);
				Test_RecurringTransaction(accountId, categoryId);
				Test_Receipt(transId);

				log.AppendLine("\n===========================================");
				log.AppendLine("         ALL TESTS COMPLETED ✓             ");
				log.AppendLine("===========================================");
			}
			catch (Exception ex)
			{
				log.AppendLine("\n[CRASH] Unexpected error: " + ex.Message);
			}

		private void RunTests_Click(object sender, RoutedEventArgs e)
		{
			log.Clear();
			Log("========== FINANCY FULL TEST ==========\n");

			// ── 1. REGISTER USER ──────────────────────────────────────
			Log("--- 1. Registering new user ---");
			User user = new User("TestUser", "chimc8699@gmail.com", "Password123");
			Log("User created: " + user);
			// ── 1B. VERIFY ACCOUNT ────────────────────────────────────
			Log("\n--- 1B. Verify account ---");
			Log("Check your email for the verification code, then enter it below.");
			string verificationCode = Microsoft.VisualBasic.Interaction.InputBox("Enter your verification code:", "Account Verification");
			bool verified = user.VerifyAccount(verificationCode);
			Log("Account verified (should be true): " + verified);

			// ── 2. LOGIN ──────────────────────────────────────────────
			Log("\n--- 2. Login test ---");
			bool loginResult = user.Login("chimc8699@gmail.com", "Password123");
			Log("Login success (should be true): " + loginResult);

			bool badLogin = user.Login("chimc8699@gmail.com", "WrongPassword");
			Log("Bad login (should be false): " + badLogin);

			// ── 3. RESET PASSWORD EMAIL ───────────────────────────────
			Log("\n--- 3. Reset password (sends email) ---");
			user.ResetPassword("chimc8699@gmail.com", "TestUser");

			// ── 4. USER PROFILE - INSERT ──────────────────────────────
			Log("\n--- 4. Insert user profile ---");
			UserProfile profile = new UserProfile();
			profile.UserID = user.UserID;
			profile.FirstName = "Test";
			profile.LastName = "User";
			profile.PhoneNumber = "0123456789";
			profile.AvatarUrl = "";
			profile.PreferredCurrency = Account.ExtractCurrencyCode("EUR - € - Euro");
			Data db = new Data();
			db.InsertProfile(profile);
			Log("Profile inserted. Full name: " + profile.GetFullName());
			Log("Preferred currency: " + profile.PreferredCurrency);

			// ── 5. USER PROFILE - UPDATE ──────────────────────────────
			Log("\n--- 5. Update user profile ---");
			profile.FirstName = "UpdatedTest";
			profile.PreferredCurrency = Account.ExtractCurrencyCode("USD - $ - US Dollar");
			profile.Save();
			Log("Profile updated. Full name: " + profile.GetFullName());
			Log("Preferred currency updated to: " + profile.PreferredCurrency);

			// ── 6. UPLOAD AVATAR ──────────────────────────────────────
			Log("\n--- 6. Upload avatar ---");
			profile.UploadAvatar(@"C:\Users\namoq\Pictures\phpto.png");
			Log("Avatar URL set to: " + profile.AvatarUrl);

			// ── 7. GET CURRENCIES LIST ────────────────────────────────
			Log("\n--- 7. Get currencies list ---");
			List<string> currencies = Account.GetCurrencies();
			Log("Total currencies loaded: " + currencies.Count);
			Log("First currency: " + currencies[0]);
			Log("Last currency: " + currencies[currencies.Count - 1]);

			// ── 8. CREATE ACCOUNT ─────────────────────────────────────
			Log("\n--- 8. Creating account ---");
			Account account = new Account(user.UserID, "My Wallet", "Personal", 500m, "EUR - € - Euro");
			bool accountSaved = account.Save();
			Log("Account saved (should be true): " + accountSaved);
			Log(account.ToString());

			// ── 9. UPDATE BALANCE ─────────────────────────────────────
			Log("\n--- 9. Updating balance ---");
			account.UpdateBalance(200m);
			Log("Balance after +200 (should be 700): " + account.Balance);
			account.UpdateBalance(-50m);
			Log("Balance after -50 (should be 650): " + account.Balance);

			// ── 10. RENAME ACCOUNT ────────────────────────────────────
			Log("\n--- 10. Renaming account ---");
			account.Rename("Main Wallet");
			Log("Account renamed to: " + account.Name);

			// ── 11. CREATE CATEGORY ───────────────────────────────────
			Log("\n--- 11. Creating category ---");
			Category category = new Category(user.UserID, "Food", "Expense");
			bool categorySaved = category.Create();
			Log("Category saved (should be true): " + categorySaved);

			// ── 12. UPDATE CATEGORY ───────────────────────────────────
			Log("\n--- 12. Updating category ---");
			category.Update("Groceries");
			Log("Category renamed to: " + category.Name);

			// ── 13. GET DEFAULT CATEGORIES ────────────────────────────
			Log("\n--- 13. Get default categories ---");
			List<Category> defaultCats = db.GetDefaultCategories();
			Log("Default categories count: " + defaultCats.Count);
			foreach (Category c in defaultCats)
				Log("  " + c.Name + " (" + c.Type + ")");

			// ── 14. CREATE TRANSACTION (EXPENSE) ─────────────────────
			Log("\n--- 14. Creating expense transaction ---");
			Transaction transaction = new Transaction(user.UserID, account.AccountID, category.CategoryID, "Expense", 75m, "Weekly groceries");
			bool transactionSaved = transaction.Create();
			Log("Transaction saved (should be true): " + transactionSaved);
			Log(transaction.ToString());

			// ── 15. CREATE TRANSACTION (INCOME) ──────────────────────
			Log("\n--- 15. Creating income transaction ---");
			Transaction income = new Transaction(user.UserID, account.AccountID, category.CategoryID, "Income", 1000m, "Monthly salary");
			bool incomeSaved = income.Create();
			Log("Income saved (should be true): " + incomeSaved);
			Log(income.ToString());

			// ── 16. CATEGORIZE TRANSACTION ────────────────────────────
			Log("\n--- 16. Categorizing transaction ---");
			transaction.Categorize(category.CategoryID);
			Log("Transaction categorized to CategoryID: " + transaction.CategoryID);

			// ── 17. UPDATE TRANSACTION ────────────────────────────────
			Log("\n--- 17. Updating transaction ---");
			transaction.Amount = 80m;
			transaction.Description = "Updated groceries";
			transaction.Update();
			Log("Transaction updated: " + transaction.ToString());

			// ── 18. GET TRANSACTION HISTORY ───────────────────────────
			Log("\n--- 18. Transaction history for account ---");
			List<Transaction> history = account.GetTransactionHistory();
			Log("Total transactions in account (should be 2): " + history.Count);
			foreach (Transaction t in history)
				Log("  " + t.ToString());

			// ── 19. GET TRANSACTIONS BY CATEGORY ─────────────────────
			Log("\n--- 19. Transactions by category ---");
			List<Transaction> catTransactions = category.GetTransactions();
			Log("Transactions in category: " + catTransactions.Count);

			// ── 20. USER MONTHLY REPORT - PDF ─────────────────────────
			Log("\n--- 20. User monthly report (PDF) ---");
			UserReport userReport = new UserReport(user.UserID, user.Username);
			userReport.GeneratePDF(DateTime.Now.Month, DateTime.Now.Year);
			Log("PDF report generated — check Downloads folder.");

			// ── 21. USER MONTHLY REPORT - EXCEL ──────────────────────
			Log("\n--- 21. User monthly report (Excel) ---");
			userReport.GenerateExcel(DateTime.Now.Month, DateTime.Now.Year);
			Log("Excel report generated — check Downloads folder.");

			// ── 22. UPDATE ROLE ───────────────────────────────────────
			Log("\n--- 22. Update role ---");
			Admin adminForRole = new Admin();
			adminForRole.Username = "AdminTester";
			user.UpdateRole(adminForRole, UserRole.Admin);
			Log("User role updated to: " + user.Role);

			// ── 23. ADMIN - GET ALL USERS ─────────────────────────────
			Log("\n--- 23. Admin get all users ---");
			Admin admin = new Admin();
			admin.Username = user.Username;
			List<User> allUsers = admin.GetAllUsers();
			Log("Total users in DB: " + allUsers.Count);

			// ── 24. ADMIN - VIEW ALL TRANSACTIONS ────────────────────
			Log("\n--- 24. Admin view all transactions ---");
			List<Transaction> allTransactions = admin.ViewAllTransactions();
			Log("Total transactions in DB: " + allTransactions.Count);

			// ── 25. ADMIN - GENERATE REPORT ───────────────────────────
			Log("\n--- 25. Admin generate report (Downloads) ---");
			admin.GenerateReport();
			Log("Admin report generated — check Downloads folder.");

			// ── 26. ADMIN - RESET USER PASSWORD ──────────────────────
			Log("\n--- 26. Admin reset user password ---");
			admin.ResetUserPassword(user.UserID);
			Log("User password reset by admin.");

			// ── 27. ADMIN - DEACTIVATE USER ───────────────────────────
			Log("\n--- 27. Admin deactivate user ---");
			admin.DeactivateUser(user.UserID);
			Log("User deactivated by admin.");

			// ── 28. DELETE TRANSACTION ────────────────────────────────
			Log("\n--- 28. Deleting transactions ---");
			transaction.Delete();
			income.Delete();
			Log("Transactions deleted.");

			// ── 29. DELETE CATEGORY ───────────────────────────────────
			Log("\n--- 29. Deleting category ---");
			category.Delete();
			Log("Category deleted.");

			// ── 30. DELETE ACCOUNT ────────────────────────────────────
			Log("\n--- 30. Deleting account ---");
			account.Delete();
			Log("Account deleted.");

			// ── 31. DEACTIVATE USER ───────────────────────────────────
			Log("\n--- 31. Deactivating user ---");
			user.Deactivate();
			Log("User active status (should be false): " + user.IsActive);

			// ── 32. ADMIN DELETE USER ─────────────────────────────────
			Log("\n--- 32. Admin delete user ---");
			db.DeleteProfile(user.UserID);  // <-- add this line
			admin.DeleteUser(user.UserID);
			Log("User deleted from DB.");

			// ── 33. LOGOUT ────────────────────────────────────────────
			Log("\n--- 33. Logout ---");
			user.Logout();

			Log("\n========== ALL TESTS DONE ==========");
		}

		private void Test_Budget(int userId, int categoryId)
		{
			log.AppendLine("\n--- [15] BUDGET ---");

			Budget budget = new Budget(0, userId, categoryId, 500m, DateTime.Now.ToString("yyyy-MM"));
			int budgetId = budget.Create();
			budget.BudgetId = budgetId;

			log.AppendLine(budgetId > 0
				? $"  PASS  Budget created. BudgetID = {budgetId}"
				: "  FAIL  Budget was not Created.");

			decimal spent = budget.GetSpentAmount();
			log.AppendLine($" PASS  GetSpentAmount() returned {spent}");

			decimal remaining = budget.GetRemainingAmount();
			log.AppendLine($" PASS  GetRemainingAmount() returned {remaining}");

			budget.Update(750m);
			log.AppendLine(" PASS  Budget Updateed to €750.");

			log.AppendLine(budget.IsExceeded()
				? "  INFO  Budget is  exceeded."
				: "  INFO  Budget is not exceeded.");
		}

        private void Test_RecurringTransaction(int accountId, int categoryId)
        {
            log.AppendLine("\n--- [16] RECURRING TRANSACTION ---");

            RecurringTransaction recurring = new RecurringTransaction(
                0,
                accountId,
                categoryId,
                "Expense",
                25m,
                "Monthly",
                DateTime.Now
            );

            int recurringId = recurring.Create();
            recurring.RecurringId = recurringId;

            log.AppendLine(recurringId > 0
                ? $"  PASS  Recurring transaction created. RecurringID = {recurringId}"
                : "  FAIL  Recurring transaction was not created.");

            DateTime oldDate = recurring.NextRunDate;
            recurring.Execute();

            log.AppendLine(recurring.NextRunDate > oldDate
                ? $"  PASS  Execute() updated NextRunDate to {recurring.NextRunDate}."
                : "  FAIL  Execute() did not update NextRunDate.");

            recurring.Pause();
            log.AppendLine(recurring.IsActive == false
                ? "  PASS  Pause() set IsActive = false."
                : "  FAIL  Pause() did not set IsActive to false.");

            recurring.Resume();
            log.AppendLine(recurring.IsActive
                ? "  PASS  Resume() set IsActive = true."
                : "  FAIL  Resume() did not set IsActive to true.");
        }

        private void Test_Receipt(int transactionId)
		{
			log.AppendLine("\n--- [17] RECEIPT ---");
			Receipt receipt = new Receipt(0, transactionId, @"C:\fake\receipt.jpg", "jpg");
			int receiptId = receipt.Upload();
			receipt.ReceiptID = receiptId;

			log.AppendLine(receiptId > 0
				? $"  PASS  Receipt uploaded. ReceiptID = {receiptId}"
				: "  FAIL  Receipt was not uploaded.");

			string url = receipt.GetDownloadUrl();
			log.AppendLine(url == receipt.FilePath
				? $"  PASS  GetDownloadUrl() returned {url}"
				: "  FAIL  GetDownloadUrl() returned wrong value.");
		}
	}
}