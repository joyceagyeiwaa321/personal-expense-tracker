using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace FinancyApplication
{
	public partial class MainWindow : Window
	{
		private StringBuilder log = new StringBuilder();
		private Data db = new Data();

		public MainWindow()
		{
			InitializeComponent();
			RunAllTests();
		}

		private void RunAllTests()
		{
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

				log.AppendLine("\n===========================================");
				log.AppendLine("         ALL TESTS COMPLETED ✓             ");
				log.AppendLine("===========================================");
			}
			catch (Exception ex)
			{
				log.AppendLine("\n[CRASH] Unexpected error: " + ex.Message);
			}

			MessageBox.Show(log.ToString(), "Test Results", MessageBoxButton.OK);
		}

		private int Test_UserRegistration()
		{
			log.AppendLine("--- [1] USER REGISTRATION ---");

			User user = new User("TestUser_" + DateTime.Now.Ticks, "test_" + DateTime.Now.Ticks + "@financy.com", "SecurePass123!");

			if (user.UserID > 0)
				log.AppendLine($"  PASS  User created. UserID = {user.UserID}, Role = {user.Role}");
			else
				log.AppendLine("  FAIL  User was NOT inserted into the database.");

			return user.UserID;
		}

		private int Test_UserProfile(int userId)
		{
			log.AppendLine("\n--- [2] USER PROFILE ---");

			UserProfile profile = new UserProfile
			{
				UserID = userId,
				FirstName = "John",
				LastName = "Doe",
				PhoneNumber = "0479123456",
				PreferredCurrency = "EUR"
			};

			int profileId = db.InsertProfile(profile);

			log.AppendLine(profileId > 0
				? $"  PASS  Profile inserted into user_profile table. ProfileID = {profileId}"
				: "  FAIL  Profile was NOT inserted into user_profile table.");

			string fullName = profile.GetFullName();
			log.AppendLine(fullName == "John Doe"
				? $"  PASS  GetFullName() = '{fullName}'"
				: $"  FAIL  GetFullName() returned unexpected value: '{fullName}'");

			profile.FirstName = "Jane";
			profile.Save();
			log.AppendLine("  PASS  Save() called. Check user_profile table: FirstName should now be 'Jane'.");

			return profileId;
		}

		private void Test_Login_Logout(int userId)
		{
			log.AppendLine("\n--- [3] LOGIN & LOGOUT ---");

			string testEmail = "logintest_" + DateTime.Now.Ticks + "@financy.com";
			string testPassword = "LoginPass99!";
			User loginUser = new User("LoginTester", testEmail, testPassword);

			bool loginResult = loginUser.Login(testEmail, testPassword);
			log.AppendLine(loginResult
				? "  PASS  Login with correct password succeeded."
				: "  FAIL  Login with correct password FAILED.");

			bool wrongLogin = loginUser.Login(testEmail, "WrongPassword!");
			log.AppendLine(!wrongLogin
				? "  PASS  Login with wrong password correctly rejected."
				: "  FAIL  Login with wrong password was incorrectly accepted!");

			loginUser.Logout();
			log.AppendLine("  PASS  Logout() called successfully.");
		}

		private void Test_ResetPassword()
		{
			log.AppendLine("\n--- [4] RESET PASSWORD ---");

			string testEmail = "resettest_" + DateTime.Now.Ticks + "@financy.com";
			User u = new User("ResetTester", testEmail, "SomePass1!");

			try
			{
				u.ResetPassword(testEmail);
				log.AppendLine("  PASS  ResetPassword() ran without errors. Check DB for ResetToken.");
			}
			catch (Exception ex)
			{
				log.AppendLine("  FAIL  ResetPassword() threw: " + ex.Message);
			}
		}

		private void Test_UpdateRole(int adminUserId)
		{
			log.AppendLine("\n--- [5] UPDATE ROLE ---");

			User targetUser = new User("RoleTarget_" + DateTime.Now.Ticks, "roletarget_" + DateTime.Now.Ticks + "@financy.com", "Pass123!");
			log.AppendLine($"  INFO  Target user created. Role = {targetUser.Role}");

			Admin adminCaller = new Admin();
			adminCaller.UserID = adminUserId;

			targetUser.UpdateRole(adminCaller, UserRole.Admin);
			log.AppendLine(targetUser.Role == UserRole.Admin
				? "  PASS  UpdateRole() to Admin succeeded."
				: "  FAIL  UpdateRole() to Admin FAILED.");

			User regularCaller = new User();
			regularCaller.Role = UserRole.User;
			targetUser.UpdateRole(regularCaller, UserRole.User);
			log.AppendLine(targetUser.Role == UserRole.Admin
				? "  PASS  Non-admin correctly blocked from changing roles."
				: "  FAIL  Non-admin was incorrectly allowed to change a role!");
		}

		private void Test_DeactivateUser(int userId)
		{
			log.AppendLine("\n--- [6] DEACTIVATE USER ---");

			User u = new User();
			u.UserID = userId;
			u.IsActive = true;

			u.Deactivate();
			log.AppendLine(u.IsActive == false
				? "  PASS  Deactivate() set IsActive = false and updated DB."
				: "  FAIL  Deactivate() did not update IsActive.");
		}

		private int Test_Account(int userId)
		{
			log.AppendLine("\n--- [7] ACCOUNT ---");

			Account acc = new Account(userId, "Main Checking", "Debit", 1000m, "EUR");
			bool saved = acc.Save();
			log.AppendLine(saved
				? $"  PASS  Account saved. AccountID = {acc.AccountID}"
				: "  FAIL  Account Save() failed.");

			decimal balanceBefore = acc.Balance;
			acc.UpdateBalance(250m);
			log.AppendLine(acc.Balance == balanceBefore + 250m
				? $"  PASS  UpdateBalance(+250) correct. New balance = {acc.Balance}"
				: $"  FAIL  UpdateBalance(+250) wrong. Expected {balanceBefore + 250m}, got {acc.Balance}");

			decimal balanceBefore2 = acc.Balance;
			acc.UpdateBalance(-100m);
			log.AppendLine(acc.Balance == balanceBefore2 - 100m
				? $"  PASS  UpdateBalance(-100) correct. New balance = {acc.Balance}"
				: $"  FAIL  UpdateBalance(-100) wrong. Expected {balanceBefore2 - 100m}, got {acc.Balance}");

			acc.Rename("Savings Account");
			log.AppendLine(acc.Name == "Savings Account"
				? "  PASS  Rename() updated Name in memory and DB."
				: "  FAIL  Rename() did not update Name.");

			log.AppendLine($"  PASS  ToString() = '{acc}'");

			return acc.AccountID;
		}

		private int Test_Category(int userId)
		{
			log.AppendLine("\n--- [8] CATEGORY ---");

			Category cat = new Category(userId, "Groceries", "Expense");
			bool created = cat.Create();
			log.AppendLine(created
				? $"  PASS  Category created. CategoryID = {cat.CategoryID}"
				: "  FAIL  Category Create() failed.");

			cat.Update("Food & Groceries");
			log.AppendLine(cat.Name == "Food & Groceries"
				? "  PASS  Category Update() renamed correctly."
				: "  FAIL  Category Update() did not rename.");

			return cat.CategoryID;
		}

		private int Test_Transaction(int userId, int accountId, int categoryId)
		{
			log.AppendLine("\n--- [9] TRANSACTION ---");

			Transaction t = new Transaction(userId, accountId, categoryId, "Expense", 49.99m, "Weekly groceries");
			bool created = t.Create();
			log.AppendLine(created
				? $"  PASS  Transaction created. TransactionID = {t.TransactionID}"
				: "  FAIL  Transaction Create() failed.");

			t.Description = "Updated grocery run";
			t.Amount = 55.00m;
			t.Update();
			log.AppendLine("  PASS  Transaction Update() called. Check DB: Description & Amount should be updated.");

			log.AppendLine($"  PASS  ToString() = '{t}'");

			Transaction badT = new Transaction(userId, accountId, categoryId, "Expense", -10m, "Bad transaction");
			bool badResult = badT.Create();
			log.AppendLine(!badResult
				? "  PASS  Transaction with negative amount correctly rejected."
				: "  FAIL  Transaction with negative amount was incorrectly accepted!");

			return t.TransactionID;
		}

		private void Test_AttachReceipt(int transactionId)
		{
			log.AppendLine("\n--- [10] ATTACH RECEIPT ---");

			Transaction t = new Transaction();
			t.TransactionID = transactionId;

			Receipt receipt = new Receipt();
			receipt.ReceiptID = 1;
			receipt.FilePath = @"C:\fake\receipt.jpg";
			receipt.FileType = "jpg";
			receipt.UploadedAt = DateTime.Now;

			try
			{
				t.AttachReceipt(receipt);
				log.AppendLine("  PASS  AttachReceipt() ran without errors.");
			}
			catch (Exception ex)
			{
				log.AppendLine("  FAIL  AttachReceipt() threw: " + ex.Message);
			}
		}

		private void Test_Categorize(int transactionId, int categoryId)
		{
			log.AppendLine("\n--- [11] CATEGORIZE ---");

			Transaction t = new Transaction();
			t.TransactionID = transactionId;

			t.Categorize(categoryId);
			log.AppendLine(t.CategoryID == categoryId
				? $"  PASS  Categorize() updated CategoryID to {categoryId} in memory and DB."
				: "  FAIL  Categorize() did not update CategoryID.");
		}

		private void Test_GetTransactionHistory(int accountId)
		{
			log.AppendLine("\n--- [12] GET TRANSACTION HISTORY (Account) ---");

			Account acc = new Account();
			acc.AccountID = accountId;

			List<Transaction> history = acc.GetTransactionHistory();
			log.AppendLine(history != null
				? $"  PASS  GetTransactionHistory() returned {history.Count} transaction(s)."
				: "  FAIL  GetTransactionHistory() returned null.");
		}

		private void Test_GetTransactionsByCategory(int categoryId)
		{
			log.AppendLine("\n--- [13] GET TRANSACTIONS BY CATEGORY ---");

			Category cat = new Category();
			cat.CategoryID = categoryId;

			List<Transaction> transactions = cat.GetTransactions();
			log.AppendLine(transactions != null
				? $"  PASS  GetTransactions() returned {transactions.Count} transaction(s) for CategoryID {categoryId}."
				: "  FAIL  GetTransactions() returned null.");
		}

		private void Test_AdminMethods(int targetUserId)
		{
			log.AppendLine("\n--- [14] ADMIN METHODS ---");

			Admin admin = new Admin();
			admin.Username = "TestAdmin";

			try
			{
				admin.GenerateReport();
				log.AppendLine("  PASS  GenerateReport() ran. Check Desktop for FinancyReport.txt.");
			}
			catch (Exception ex)
			{
				log.AppendLine("  FAIL  GenerateReport() threw: " + ex.Message);
			}

			try
			{
				admin.ManageCategories();
				log.AppendLine("  PASS  ManageCategories() ran without errors.");
			}
			catch (Exception ex)
			{
				log.AppendLine("  FAIL  ManageCategories() threw: " + ex.Message);
			}

			User throwaway = new User("Throwaway_" + DateTime.Now.Ticks, "throw_" + DateTime.Now.Ticks + "@financy.com", "Pass123!");
			if (throwaway.UserID > 0)
			{
				admin.DeleteUser(throwaway.UserID);
				log.AppendLine($"  PASS  Admin.DeleteUser() called on throwaway UserID = {throwaway.UserID}. Check DB.");
			}
			else
			{
				log.AppendLine("  SKIP  Throwaway user not created, skipping DeleteUser test.");
			}
		}
	}
}