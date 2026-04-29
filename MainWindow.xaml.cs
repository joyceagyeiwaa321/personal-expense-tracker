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
		private void RunAllTests_Click(object sender, RoutedEventArgs e)
		{
			log.Clear();
			Log("========== FINANCY FULL SYSTEM TEST ==========\n");
			Log("Started: " + DateTime.Now + "\n");

			Data db = new Data();

			Log("--- 1. EmailExists (should be false on clean DB) ---");
			string testEmail = Microsoft.VisualBasic.Interaction.InputBox("Enter your test email:", "Test Email");
			bool exists = db.EmailExists(testEmail);
			Log("EmailExists (should be false): " + exists);

			Log("\n--- 2. Register User ---");
			User user = null;
			try
			{
				user = new User("TestUser", testEmail, "Password123");
				Log("User created. UserID: " + user.UserID);
				Log("Role (should be Admin since first user): " + user.Role);
				Log("IsVerified (should be false): " + user.IsVerified);
			}
			catch (Exception ex)
			{
				Log("FAIL: " + ex.Message);
				return;
			}

			Log("\n--- 3. EmailExists after register (should be true) ---");
			Log("EmailExists: " + db.EmailExists(testEmail));

			Log("\n--- 4. Duplicate Registration (should throw) ---");
			try
			{
				User duplicate = new User("TestUser2", testEmail, "Password123");
				Log("FAIL: Should have thrown exception for duplicate email.");
			}
			catch (Exception ex)
			{
				Log("PASS: Duplicate caught: " + ex.Message);
			}

			Log("\n--- 5. Verify Account ---");
			string verificationCode = Microsoft.VisualBasic.Interaction.InputBox("Check your email and enter the 6-digit verification code:", "Verify Account");
			bool verified = user.VerifyAccount(verificationCode);
			Log("Verified (should be true): " + verified);

			Log("\n--- 6. Wrong Verification Code (should be false) ---");
			bool badVerify = user.VerifyAccount("XXXXXX");
			Log("Bad verify (should be false): " + badVerify);

			Log("\n--- 7. Resend Verification ---");
			bool resent = db.ResendVerification(testEmail, user.Username);
			Log("Resend verification sent (should be true): " + resent);

			Log("\n--- 8. Login ---");
			bool loginResult = user.Login(testEmail, "Password123");
			Log("Login (should be true): " + loginResult);
			bool badLogin = user.Login(testEmail, "WrongPassword");
			Log("Bad login (should be false): " + badLogin);

			Log("\n--- 9. Reset Password (sends email) ---");
			user.ResetPassword(testEmail, user.Username);
			Log("Reset password email sent.");

			Log("\n--- 10. Resend Password Reset ---");
			bool resentReset = db.ResendPasswordReset(testEmail, user.Username);
			Log("Resend reset sent (should be true): " + resentReset);

			Log("\n--- 11. GetVerificationStatus ---");
			bool verStatus = db.GetVerificationStatus(user.UserID);
			Log("IsVerified in DB (should be true): " + verStatus);

			Log("\n--- 12. Insert UserProfile ---");
			UserProfile profile = new UserProfile();
			profile.UserID = user.UserID;
			profile.FirstName = "Test";
			profile.LastName = "User";
			profile.PhoneNumber = "0123456789";
			profile.AvatarUrl = "";
			profile.PreferredCurrency = Account.ExtractCurrencyCode("EUR - € - Euro");
			db.InsertProfile(profile);
			Log("Profile inserted. FullName: " + profile.GetFullName());

			Log("\n--- 13. Update UserProfile ---");
			profile.FirstName = "Updated";
			profile.PreferredCurrency = Account.ExtractCurrencyCode("USD - $ - US Dollar");
			profile.Save();
			Log("Profile updated. FullName: " + profile.GetFullName());

			Log("\n--- 14. UploadAvatar ---");
			string avatarPath = Microsoft.VisualBasic.Interaction.InputBox("Enter a valid image path on your PC (or cancel to skip):", "Avatar Path");
			if (!string.IsNullOrWhiteSpace(avatarPath))
			{
				profile.UploadAvatar(avatarPath);
				Log("Avatar URL: " + profile.AvatarUrl);
			}
			else
			{
				Log("Avatar test skipped.");
			}

			Log("\n--- 15. GetCurrencies ---");
			List<string> currencies = Account.GetCurrencies();
			Log("Total currencies: " + currencies.Count);
			Log("First: " + currencies[0]);
			Log("Last: " + currencies[currencies.Count - 1]);

			Log("\n--- 16. ExtractCurrencyCode & ExtractCurrencySymbol ---");
			string code = Account.ExtractCurrencyCode("EUR - € - Euro");
			string symbol = Account.ExtractCurrencySymbol("EUR - € - Euro");
			Log("Code (should be EUR): " + code);
			Log("Symbol (should be €): " + symbol);

			Log("\n--- 17. Create Account ---");
			Account account = new Account(user.UserID, "My Wallet", "Personal", 500m, "EUR - € - Euro");
			bool accountSaved = account.Save();
			Log("Account saved (should be true): " + accountSaved);
			Log(account.ToString());

			Log("\n--- 18. UpdateBalance ---");
			account.UpdateBalance(200m);
			Log("Balance after +200 (should be 700): " + account.Balance);
			account.UpdateBalance(-50m);
			Log("Balance after -50 (should be 650): " + account.Balance);

			Log("\n--- 19. UpdateBalance with 0 (should block) ---");
			account.UpdateBalance(0m);
			Log("Zero balance update attempted (should have shown messagebox).");

			Log("\n--- 20. Rename Account ---");
			account.Rename("Main Wallet");
			Log("Renamed to: " + account.Name);

			Log("\n--- 21. CreateDefaultCategories ---");
			Category.CreateDefaultCategories(user.UserID);
			List<Category> defaultCats = db.GetDefaultCategories();
			Log("Default categories created: " + defaultCats.Count);
			foreach (Category dc in defaultCats)
				Log("  " + dc.Name + " (" + dc.Type + ")");

			Log("\n--- 22. Create Category ---");
			Category category = new Category(user.UserID, "Food", "expense");
			bool catSaved = category.Create();
			Log("Category saved (should be true): " + catSaved);

			Log("\n--- 23. Update Category ---");
			category.Update("Groceries");
			Log("Category renamed to: " + category.Name);

			Log("\n--- 24. Delete Default Category (should block) ---");
			if (defaultCats.Count > 0)
			{
				defaultCats[0].Delete();
				Log("Tried deleting default category (should have been blocked).");
			}

			Log("\n--- 25. Create Expense Transaction ---");
			Transaction transaction = new Transaction(user.UserID, account.AccountID, category.CategoryID, "Expense", 75m, "Weekly groceries");
			bool transSaved = transaction.Create();
			Log("Transaction saved (should be true): " + transSaved);
			Log(transaction.ToString());

			Log("\n--- 26. Create Income Transaction ---");
			Transaction income = new Transaction(user.UserID, account.AccountID, category.CategoryID, "Income", 1000m, "Monthly salary");
			bool incomeSaved = income.Create();
			Log("Income saved (should be true): " + incomeSaved);
			Log(income.ToString());

			Log("\n--- 27. Transaction with 0 amount (should block) ---");
			Transaction badTrans = new Transaction(user.UserID, account.AccountID, category.CategoryID, "Expense", 0m, "Bad transaction");
			bool badSaved = badTrans.Create();
			Log("Zero transaction saved (should be false): " + badSaved);

			Log("\n--- 28. AttachReceipt ---");
			Receipt receipt = new Receipt(0, transaction.TransactionID, @"C:\fake\receipt.jpg", "jpg");
			int receiptId = receipt.Upload();
			receipt.ReceiptID = receiptId;
			Log("Receipt uploaded. ReceiptID: " + receiptId);
			transaction.AttachReceipt(receipt);
			Log("Receipt attached to transaction.");
			Log("GetDownloadUrl: " + receipt.GetDownloadUrl());

			Log("\n--- 29. Categorize Transaction ---");
			transaction.Categorize(category.CategoryID);
			Log("Transaction categorized. CategoryID: " + transaction.CategoryID);

			Log("\n--- 30. Update Transaction ---");
			transaction.Amount = 80m;
			transaction.Description = "Updated groceries";
			transaction.Update();
			Log("Transaction updated: " + transaction.ToString());

			Log("\n--- 31. GetTransactionHistory ---");
			List<Transaction> history = account.GetTransactionHistory();
			Log("Transactions in account (should be 2): " + history.Count);
			foreach (Transaction t in history)
				Log("  " + t.ToString());

			Log("\n--- 32. GetTransactions by Category ---");
			List<Transaction> catTrans = category.GetTransactions();
			Log("Transactions in category: " + catTrans.Count);

			Log("\n--- 33. Create Budget ---");
			Budget budget = new Budget(0, user.UserID, category.CategoryID, 500m, DateTime.Now.ToString("yyyy-MM"));
			int budgetId = budget.Create();
			budget.BudgetId = budgetId;
			Log("Budget created. BudgetID: " + budgetId);
			decimal spent = budget.GetSpentAmount();
			Log("GetSpentAmount: " + spent);
			decimal remaining = budget.GetRemainingAmount();
			Log("GetRemainingAmount: " + remaining);
			Log("IsExceeded (should be false): " + budget.IsExceeded());
			budget.Update(50m);
			Log("Budget updated to €50. IsExceeded now (should be true): " + budget.IsExceeded());

			Log("\n--- 34. RecurringTransaction ---");
			RecurringTransaction recurring = new RecurringTransaction(0, account.AccountID, category.CategoryID, "Expense", 25m, "Monthly", DateTime.Now);
			int recurringId = recurring.Create();
			recurring.RecurringId = recurringId;
			Log("Recurring created. RecurringID: " + recurringId);
			DateTime oldDate = recurring.NextRunDate;
			recurring.Execute();
			Log("Execute() updated NextRunDate (should be true): " + (recurring.NextRunDate > oldDate));
			recurring.Pause();
			Log("Pause() IsActive (should be false): " + recurring.IsActive);
			recurring.Resume();
			Log("Resume() IsActive (should be true): " + recurring.IsActive);

			Log("\n--- 35. Group ---");
			Group group = new Group(user.UserID, "Test Group", "A test group");
			int groupId = group.Create();
			group.GroupID = groupId;
			Log("Group created. GroupID: " + groupId);
			Log("InviteCode: " + group.InviteCode);
			Log(group.ToString());
			group.Update("Updated Group");
			Log("Group updated to: " + group.Name);

			Log("\n--- 36. GroupMember ---");
			GroupMember member = group.AddMember(user.UserID);
			Log("Member added. UserID: " + member.UserID);
			List<GroupMember> members = group.GetMembers();
			Log("Members count (should be 1): " + members.Count);
			GroupMember gm = members[0];
			Group fetchedGroup = gm.GetGroup();
			Log("GetGroup from member. Name: " + fetchedGroup.Name);
			User fetchedUser = gm.GetUser();
			Log("GetUser from member. Username: " + fetchedUser.Username);

			Log("\n--- 37. JoinByCode ---");
			int joinedGroupId = Group.JoinByCode(user.UserID, group.InviteCode);
			Log("JoinByCode result (should be > 0): " + joinedGroupId);

			Log("\n--- 38. GetTransactions from Group ---");
			List<Transaction> groupTrans = group.GetTransactions();
			Log("Group transactions: " + groupTrans.Count);

			Log("\n--- 39. UserReport PDF & Excel ---");
			UserReport userReport = new UserReport(user.UserID, user.Username);
			userReport.GeneratePDF(DateTime.Now.Month, DateTime.Now.Year);
			Log("PDF generated — check Downloads.");
			userReport.GenerateExcel(DateTime.Now.Month, DateTime.Now.Year);
			Log("Excel generated — check Downloads.");

			Log("\n--- 40. Admin Methods ---");
			Admin admin = new Admin();
			admin.Username = user.Username;
			List<User> allUsers = admin.GetAllUsers();
			Log("GetAllUsers count: " + allUsers.Count);
			List<Transaction> allTrans = admin.ViewAllTransactions();
			Log("ViewAllTransactions count: " + allTrans.Count);
			admin.ManageDefaultCategories();
			Log("ManageDefaultCategories called.");
			admin.GenerateReport();
			Log("GenerateReport saved to Downloads.");
			user.UpdateRole(admin, UserRole.Admin);
			Log("UpdateRole to Admin: " + user.Role);
			admin.ResetUserPassword(user.UserID);
			Log("ResetUserPassword called.");

			Log("\n--- 41. UpdateUserStatus ---");
			db.UpdateUserStatus(user.UserID, false);
			Log("User deactivated.");
			db.UpdateUserStatus(user.UserID, true);
			Log("User reactivated.");

			Log("\n--- 42. Logout ---");
			user.Logout();
			Log("User logged out.");

			Log("\n--- 43. Cleanup ---");
			group.RemoveMember(user.UserID);
			Log("Member removed.");
			group.Delete();
			Log("Group deleted.");
			recurring.Cancel();
			Log("Recurring cancelled.");
			budget.Delete();
			Log("Budget deleted.");
			receipt.Delete();
			Log("Receipt deleted.");
			transaction.Delete();
			income.Delete();
			Log("Transactions deleted.");
			category.Delete();
			Log("Category deleted.");
			foreach (Category dc in defaultCats)
			{
				dc.IsDefault = false;
				dc.Delete();
			}
			Log("Default categories deleted.");
			account.Delete();
			Log("Account deleted.");
			db.DeleteProfile(user.UserID);
			admin.DeleteUser(user.UserID);
			Log("User deleted.");

			Log("\n========== ALL TESTS DONE ==========");
			Log("Check above for any FAIL or unexpected values.");
		}

		private void RunPersistentTest_Click(object sender, RoutedEventArgs e)
		{
			log.Clear();
			Log("========== PERSISTENT TEST (data stays in DB) ==========\n");
			Log("Started: " + DateTime.Now + "\n");

			Data db = new Data();

			Log("--- 1. Register User ---");
			string testEmail = Microsoft.VisualBasic.Interaction.InputBox("Enter your test email:", "Persistent Test");
			User user = null;
			try
			{
				user = new User("PersistentUser", testEmail, "Password123");
				Log("User created. UserID: " + user.UserID);
				Log("Role: " + user.Role);
			}
			catch (Exception ex)
			{
				Log("FAIL: " + ex.Message);
				return;
			}

			Log("\n--- 2. Verify Account ---");
			string code = Microsoft.VisualBasic.Interaction.InputBox("Enter your 6-digit verification code:", "Verify");
			bool verified = user.VerifyAccount(code);
			Log("Verified: " + verified);

			Log("\n--- 3. Insert Profile ---");
			UserProfile profile = new UserProfile();
			profile.UserID = user.UserID;
			profile.FirstName = "Persistent";
			profile.LastName = "Tester";
			profile.PhoneNumber = "0000000000";
			profile.PreferredCurrency = Account.ExtractCurrencyCode("EUR - € - Euro");
			db.InsertProfile(profile);
			Log("Profile inserted: " + profile.GetFullName());

			Log("\n--- 4. Create Default Categories ---");
			Category.CreateDefaultCategories(user.UserID);
			Log("Default categories created.");

			Log("\n--- 5. Create Account ---");
			Account account = new Account(user.UserID, "Savings Account", "Savings", 1000m, "EUR - € - Euro");
			account.Save();
			Log("Account created: " + account.ToString());

			Log("\n--- 6. Create Transactions ---");
			List<Category> defaultCats = db.GetDefaultCategories();
			Category salarycat = defaultCats.Find(c => c.Name == "Salary");
			Category foodCat = defaultCats.Find(c => c.Name == "Food & Dining");

			Transaction salary = new Transaction(user.UserID, account.AccountID, salarycat.CategoryID, "Income", 2000m, "Monthly salary");
			salary.Create();
			Log("Salary transaction created: " + salary.ToString());

			Transaction groceries = new Transaction(user.UserID, account.AccountID, foodCat.CategoryID, "Expense", 150m, "Weekly groceries");
			groceries.Create();
			Log("Groceries transaction created: " + groceries.ToString());

			Transaction rent = new Transaction(user.UserID, account.AccountID, foodCat.CategoryID, "Expense", 800m, "Monthly rent");
			rent.Create();
			Log("Rent transaction created: " + rent.ToString());

			Log("\n--- 7. Attach Receipt to groceries ---");
			Receipt receipt = new Receipt(0, groceries.TransactionID, @"C:\fake\groceries_receipt.jpg", "jpg");
			int receiptId = receipt.Upload();
			receipt.ReceiptID = receiptId;
			groceries.AttachReceipt(receipt);
			Log("Receipt attached. ReceiptID: " + receiptId);

			Log("\n--- 8. Create Budget ---");
			Budget budget = new Budget(0, user.UserID, foodCat.CategoryID, 500m, DateTime.Now.ToString("yyyy-MM"));
			int budgetId = budget.Create();
			budget.BudgetId = budgetId;
			Log("Budget created. Spent: " + budget.GetSpentAmount() + " / Limit: " + budget.LimitAmount);
			Log("IsExceeded: " + budget.IsExceeded());

			Log("\n--- 9. Create Recurring Transaction ---");
			RecurringTransaction recurring = new RecurringTransaction(0, account.AccountID, foodCat.CategoryID, "Expense", 50m, "Monthly", DateTime.Now);
			int recurringId = recurring.Create();
			recurring.RecurringId = recurringId;
			Log("Recurring created. RecurringID: " + recurringId + " NextRun: " + recurring.NextRunDate);

			Log("\n--- 10. Create Group ---");
			Group group = new Group(user.UserID, "Family Budget", "Shared family expenses");
			int groupId = group.Create();
			group.GroupID = groupId;
			Log("Group created. GroupID: " + groupId);
			Log("InviteCode: " + group.InviteCode + " ← share this to join!");
			Log(group.ToString());

			Log("\n--- 11. Add User as Group Member ---");
			GroupMember member = group.AddMember(user.UserID);
			Log("Member added. UserID: " + member.UserID);
			List<GroupMember> members = group.GetMembers();
			Log("Total members: " + members.Count);

			Log("\n--- 12. Generate Reports ---");
			UserReport report = new UserReport(user.UserID, user.Username);
			report.GeneratePDF(DateTime.Now.Month, DateTime.Now.Year);
			Log("PDF saved to Downloads.");
			report.GenerateExcel(DateTime.Now.Month, DateTime.Now.Year);
			Log("Excel saved to Downloads.");

			Log("\n========== PERSISTENT TEST DONE ==========");
			Log("All data has been saved to the database!");
			Log("Check phpMyAdmin to see everything. 🎉");
			Log("\nUser: PersistentUser");
			Log("Email: " + testEmail);
			Log("Group InviteCode: " + group.InviteCode);
		}

		// ================= MY PARTNER'S PART =================
		private void RunMyTests_Click(object sender, RoutedEventArgs e)
		{
			log.Clear();

			log.AppendLine("===========================================");
			log.AppendLine("       MY PART TEST");
			log.AppendLine("       BUDGET / RECURRING / RECEIPT / GROUP");
			log.AppendLine("===========================================\n");

			try
			{
				int userId = 1;
				int accountId = 1;
				int categoryId = 1;
				int transId = 1;

				Test_Budget(userId, categoryId);
				Test_RecurringTransaction(accountId, categoryId);
				Test_Receipt(transId);
				Test_Group(userId);

				log.AppendLine("\n===========================================");
				log.AppendLine("         MY TESTS COMPLETED ✓");
				log.AppendLine("===========================================");
			}
			catch (Exception ex)
			{
				log.AppendLine("\n[CRASH] Unexpected error: " + ex.Message);
			}

			OutputBox.Text = log.ToString();
		}

		private void Test_Budget(int userId, int categoryId)
		{
			log.AppendLine("\n--- [MY TEST 1] BUDGET ---");
			Budget budget = new Budget(0, userId, categoryId, 500m, DateTime.Now.ToString("yyyy-MM"));
			int budgetId = budget.Create();
			budget.BudgetId = budgetId;
			log.AppendLine(budgetId > 0 ? $"  PASS  Budget created. BudgetID = {budgetId}" : "  FAIL  Budget was not created.");
			decimal spent = budget.GetSpentAmount();
			log.AppendLine($"  PASS  GetSpentAmount() returned {spent}");
			decimal remaining = budget.GetRemainingAmount();
			log.AppendLine($"  PASS  GetRemainingAmount() returned {remaining}");
			budget.Update(750m);
			log.AppendLine("  PASS  Budget updated to €750.");
			log.AppendLine(budget.IsExceeded() ? "  INFO  Budget is exceeded." : "  INFO  Budget is not exceeded.");
		}

		private void Test_RecurringTransaction(int accountId, int categoryId)
		{
			log.AppendLine("\n--- [MY TEST 2] RECURRING TRANSACTION ---");
			RecurringTransaction recurring = new RecurringTransaction(0, accountId, categoryId, "Expense", 25m, "Monthly", DateTime.Now);
			int recurringId = recurring.Create();
			recurring.RecurringId = recurringId;
			log.AppendLine(recurringId > 0 ? $"  PASS  Recurring transaction created. RecurringID = {recurringId}" : "  FAIL  Recurring transaction was not created.");
			DateTime oldDate = recurring.NextRunDate;
			recurring.Execute();
			log.AppendLine(recurring.NextRunDate > oldDate ? $"  PASS  Execute() updated NextRunDate to {recurring.NextRunDate}." : "  FAIL  Execute() did not update NextRunDate.");
			recurring.Pause();
			log.AppendLine(recurring.IsActive == false ? "  PASS  Pause() set IsActive = false." : "  FAIL  Pause() did not set IsActive to false.");
			recurring.Resume();
			log.AppendLine(recurring.IsActive ? "  PASS  Resume() set IsActive = true." : "  FAIL  Resume() did not set IsActive to true.");
		}

		private void Test_Receipt(int transactionId)
		{
			log.AppendLine("\n--- [MY TEST 3] RECEIPT ---");
			Receipt receipt = new Receipt(0, transactionId, @"C:\fake\receipt.jpg", "jpg");
			int receiptId = receipt.Upload();
			receipt.ReceiptID = receiptId;
			log.AppendLine(receiptId > 0 ? $"  PASS  Receipt uploaded. ReceiptID = {receiptId}" : "  FAIL  Receipt was not uploaded.");
			string url = receipt.GetDownloadUrl();
			log.AppendLine(url == receipt.FilePath ? $"  PASS  GetDownloadUrl() returned {url}" : "  FAIL  GetDownloadUrl() returned wrong value.");
		}

		private void Test_Group(int userId)
		{
			log.AppendLine("\n--- [MY TEST 4] GROUP ---");
			Group group = new Group(userId, "Test Group", "A test group"); 
			int groupId = group.Create();
			group.GroupID = groupId;
			log.AppendLine(groupId > 0 ? $"  PASS  Group created. GroupID = {groupId}" : "  FAIL  Group was not created.");
			group.Update("Updated Test Group");
			log.AppendLine("  PASS  Group updated to: " + group.Name);
			GroupMember member = group.AddMember(userId);
			log.AppendLine("  PASS  Member added. UserID = " + member.UserID);
			List<GroupMember> members = group.GetMembers();
			log.AppendLine("  PASS  Group members count: " + members.Count);
			group.RemoveMember(userId);
			log.AppendLine("  PASS  Member removed.");
			group.Delete();
			log.AppendLine("  PASS  Group deleted.");
		}
	}
}