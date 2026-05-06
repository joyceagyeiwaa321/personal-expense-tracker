using DocumentFormat.OpenXml.Spreadsheet;
using MySqlConnector;
using System;
using System.Collections.Generic;

namespace FinancyApplication
{
	public class Data
	{
		private string connectionString = "datasource=127.0.0.1;port=3308;username=root;password=;database=expense_tracker;";

		private int Insert(string query)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				MySqlCommand commandDatabase = new MySqlCommand(query, connection);
				try
				{
					connection.Open();
					commandDatabase.ExecuteNonQuery();
					return (int)commandDatabase.LastInsertedId;
				}
				catch (Exception ex)
				{
					throw new Exception("Insert failed: " + ex.Message);
				}
			}
		}

		private void ExecuteSimple(string query)
		{
			using (MySqlConnection conn = new MySqlConnection(connectionString))
			{
				MySqlCommand cmd = new MySqlCommand(query, conn);
				try
				{
					conn.Open();
					cmd.ExecuteNonQuery();
				}
				catch (Exception ex)
				{
					throw new Exception("ExecuteSimple failed: " + ex.Message);
				}
			}
		}

		public int GetUserCount()
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT COUNT(*) FROM user";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				try
				{
					connection.Open();
					return Convert.ToInt32(cmd.ExecuteScalar());
				}
				catch (Exception ex)
				{
					throw new Exception("GetUserCount failed: " + ex.Message);
				}
			}
		}

		public int GetActiveUserCount()
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT COUNT(*) FROM user WHERE IsActive = 1";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				try
				{
					connection.Open();
					return Convert.ToInt32(cmd.ExecuteScalar());
				}
				catch (Exception ex)
				{
					throw new Exception("GetActiveUserCount failed: " + ex.Message);
				}
			}
		}

		public List<User> GetAllUsers()
		{
			List<User> users = new List<User>();
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT * FROM user";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				try
				{
					connection.Open();
					MySqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						User u = new User();
						u.UserID = Convert.ToInt32(reader["UserID"]);
						u.Username = reader["Username"].ToString();
						u.Email = reader["Email"].ToString();
						if (reader["Role"].ToString() == "Admin")
						{
							u.Role = UserRole.Admin;
						}
						else
						{
							u.Role = UserRole.User;
						}
						u.IsActive = Convert.ToInt32(reader["IsActive"]) == 1;
						u.CreatedAt = Convert.ToDateTime(reader["CreatedAt"]);
						users.Add(u);
					}
				}
				catch (Exception ex)
				{
					throw new Exception("GetAllUsers failed: " + ex.Message);
				}
			}
			return users;
		}

		public int InsertUser(User user, string password)
		{
			string query = "INSERT INTO user(Username, Email, Password, Role, CreatedAt, IsActive) VALUES('" +
						   user.Username + "', '" + user.Email + "', '" + password + "', '" +
						   user.Role + "', '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', 1);";
			return this.Insert(query);
		}

		public string GetPasswordHash(string email)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT Password FROM user WHERE Email = '" + email + "' LIMIT 1";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				try
				{
					connection.Open();
					object result = cmd.ExecuteScalar();

					if (result != null)
					{
						return result.ToString();
					}
					else
					{
						return null;
					}
				}
				catch (Exception ex)
				{
					throw new Exception("GetPasswordHash failed: " + ex.Message);
				}
			}
		}

		public bool ValidateLogin(string email, string password)
		{
			string storedHash = GetPasswordHash(email);
			if (string.IsNullOrEmpty(storedHash))
			{
				return false;
			}

			try
			{
				return BCrypt.Net.BCrypt.Verify(password, storedHash);
			}
			catch (Exception ex)
			{
				throw new Exception("ValidateLogin failed: " + ex.Message);
			}
		}

		public void UpdateResetToken(string email, string token)
		{
			string query = "UPDATE user SET ResetToken = '" + token + "' WHERE Email = '" + email + "'";
			this.ExecuteSimple(query);
		}

		public void UpdateUserStatus(int userId, bool isActive)
		{
			int status;
			if (isActive == true)
			{
				status = 1;
			}
			else
			{
				status = 0;
			}

			string query = "UPDATE user SET IsActive = " + status + " WHERE UserID = " + userId;
			this.ExecuteSimple(query);
		}

		public void UpdateUserRole(int userId, string role)
		{
			string query = "UPDATE user SET Role = '" + role + "' WHERE UserID = " + userId;
			this.ExecuteSimple(query);
		}

		public void UpdateUserPassword(int userId, string newHashedPassword)
		{
			string query = "UPDATE user SET Password = '" + newHashedPassword + "' WHERE UserID = " + userId;
			this.ExecuteSimple(query);
		}
		public void UpdateUsername(int userId, string newUsername)
		{
			string query = "UPDATE user SET Username = '" + newUsername + "' WHERE UserID = " + userId;
			this.ExecuteSimple(query);
		}


		public void DeleteUser(int userId)
		{
			string query = "DELETE FROM user WHERE UserID = " + userId;
			this.ExecuteSimple(query);
		}

		public void UpdateProfile(UserProfile profile)
		{
			string query = "UPDATE user_profile SET FirstName='" + profile.FirstName + "', LastName='" + profile.LastName +
						   "', PhoneNumber='" + profile.PhoneNumber + "', AvatarURL='" + profile.AvatarUrl +
						   "', PreferedCurrency='" + profile.PreferredCurrency + "' WHERE UserID = " + profile.UserID + ";";
			this.ExecuteSimple(query);
		}

		public int InsertProfile(UserProfile profile)
		{
			string query = "INSERT INTO user_profile(UserID, FirstName, LastName, PhoneNumber, AvatarURL, PreferedCurrency) " +
						   "VALUES(" + profile.UserID + ", '" + profile.FirstName + "', '" + profile.LastName + "', '" +
						   profile.PhoneNumber + "', '" + profile.AvatarUrl + "', '" + profile.PreferredCurrency + "');";
			return this.Insert(query);
		}

		public int InsertAccount(Account acc)
		{
			string balance = acc.Balance.ToString(System.Globalization.CultureInfo.InvariantCulture);
			string query = "INSERT INTO account(UserID, Name, AccountType, Balance, Currency, CreatedAt) VALUES(" +
						   acc.UserID + ", '" + acc.Name + "', '" + acc.AccountType + "', " + balance +
						   ", '" + acc.Currency + "', '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "');";
			return this.Insert(query);
		}

		public void UpdateAccountBalance(int accountId, decimal amount)
		{
			string amt = amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
			string query = "UPDATE account SET Balance = Balance + " + amt + " WHERE AccountID = " + accountId;
			this.ExecuteSimple(query);
		}

		public void RenameAccount(int accountId, string newName)
		{
			string query = "UPDATE account SET Name = '" + newName + "' WHERE AccountID = " + accountId;
			this.ExecuteSimple(query);
		}

		public void DeleteAccount(int accountId)
		{
			string query = "DELETE FROM account WHERE AccountID = " + accountId;
			this.ExecuteSimple(query);
		}

		public int GetTotalAccountCount()
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT COUNT(*) FROM account";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				try
				{
					connection.Open();
					return Convert.ToInt32(cmd.ExecuteScalar());
				}
				catch (Exception ex)
				{
					throw new Exception("GetTotalAccountCount failed: " + ex.Message);
				}
			}
		}

		public List<Transaction> GetTransactionsByAccount(int accountId)
		{
			List<Transaction> transactions = new List<Transaction>();
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT * FROM `transaction` WHERE AccountID = " + accountId;
				MySqlCommand cmd = new MySqlCommand(query, connection);
				try
				{
					connection.Open();
					MySqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						Transaction t = new Transaction();
						t.TransactionID = Convert.ToInt32(reader["TransactionID"]);
						t.UserID = Convert.ToInt32(reader["UserID"]);
						t.AccountID = Convert.ToInt32(reader["AccountID"]);
						t.CategoryID = Convert.ToInt32(reader["CategoryID"]);
						t.Type = reader["Type"].ToString();
						t.Amount = Convert.ToDecimal(reader["Amount"]);
						t.Description = reader["Description"].ToString();
						t.Date = Convert.ToDateTime(reader["Date"]);
						transactions.Add(t);
					}
				}
				catch (Exception ex)
				{
					throw new Exception("GetTransactionsByAccount failed: " + ex.Message);
				}
			}
			return transactions;
		}

		public List<Transaction> GetTransactionsByCategory(int categoryId)
		{
			List<Transaction> transactions = new List<Transaction>();
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT * FROM `transaction` WHERE CategoryID = " + categoryId;
				MySqlCommand cmd = new MySqlCommand(query, connection);
				try
				{
					connection.Open();
					MySqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						Transaction t = new Transaction();
						t.TransactionID = Convert.ToInt32(reader["TransactionID"]);
						t.UserID = Convert.ToInt32(reader["UserID"]);
						t.AccountID = Convert.ToInt32(reader["AccountID"]);
						t.CategoryID = Convert.ToInt32(reader["CategoryID"]);
						t.Type = reader["Type"].ToString();
						t.Amount = Convert.ToDecimal(reader["Amount"]);
						t.Description = reader["Description"].ToString();
						t.Date = Convert.ToDateTime(reader["Date"]);
						transactions.Add(t);
					}
				}
				catch (Exception ex)
				{
					throw new Exception("GetTransactionsByCategory failed: " + ex.Message);
				}
			}
			return transactions;
		}

		public List<Transaction> GetAllTransactions()
		{
			List<Transaction> transactions = new List<Transaction>();
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT * FROM `transaction` ORDER BY Date DESC";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				try
				{
					connection.Open();
					MySqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						Transaction t = new Transaction();
						t.TransactionID = Convert.ToInt32(reader["TransactionID"]);
						t.UserID = Convert.ToInt32(reader["UserID"]);
						t.AccountID = Convert.ToInt32(reader["AccountID"]);
						t.CategoryID = Convert.ToInt32(reader["CategoryID"]);
						t.Type = reader["Type"].ToString();
						t.Amount = Convert.ToDecimal(reader["Amount"]);
						t.Description = reader["Description"].ToString();
						t.Date = Convert.ToDateTime(reader["Date"]);
						transactions.Add(t);
					}
				}
				catch (Exception ex)
				{
					throw new Exception("GetAllTransactions failed: " + ex.Message);
				}
			}
			return transactions;
		}

		public List<Transaction> GetTransactionsByUser(int userId, int month, int year)
		{
			List<Transaction> transactions = new List<Transaction>();
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT * FROM `transaction` WHERE UserID = " + userId +
							   " AND MONTH(`Date`) = " + month +
							   " AND YEAR(`Date`) = " + year +
							   " ORDER BY `Date` DESC";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				try
				{
					connection.Open();
					MySqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						Transaction t = new Transaction();
						t.TransactionID = Convert.ToInt32(reader["TransactionID"]);
						t.UserID = Convert.ToInt32(reader["UserID"]);
						t.AccountID = Convert.ToInt32(reader["AccountID"]);
						t.CategoryID = Convert.ToInt32(reader["CategoryID"]);
						t.Type = reader["Type"].ToString();
						t.Amount = Convert.ToDecimal(reader["Amount"]);
						t.Description = reader["Description"].ToString();
						t.Date = Convert.ToDateTime(reader["Date"]);
						transactions.Add(t);
					}
				}
				catch (Exception ex)
				{
					throw new Exception("GetTransactionsByUser failed: " + ex.Message);
				}
			}
			return transactions;
		}

		public int GetTotalTransactionCount()
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT COUNT(*) FROM `transaction`";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				try
				{
					connection.Open();
					return Convert.ToInt32(cmd.ExecuteScalar());
				}
				catch (Exception ex)
				{
					throw new Exception("GetTotalTransactionCount failed: " + ex.Message);
				}
			}
		}

		public int InsertTransaction(Transaction t)
		{
			string amount = t.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);

			string gIDValue = "NULL";
			if (t.GroupID > 0)
			{
				gIDValue = t.GroupID.ToString();
			}

			string query = "INSERT INTO `transaction` (UserID, AccountID, CategoryID, GroupID, Type, Amount, Description, `Date`) " +
						   "VALUES (" + t.UserID + ", " + t.AccountID + ", " + t.CategoryID + ", " + gIDValue + ", '" + t.Type + "', " +
						   amount + ", '" + t.Description + "', '" +
						   t.Date.ToString("yyyy-MM-dd HH:mm:ss") + "');";
			return this.Insert(query);
		}

		public int InsertGroup(Group group)
		{
			string query = "INSERT INTO `group`(CreatedByUserID, Name, Description, InviteCode, CreatedAt) VALUES(" +
						   group.CreatedByUserID + ", '" + group.Name + "', '" + group.Description + "', '" +
						   group.InviteCode + "', '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "');";
			return this.Insert(query);
		}

		public int GetGroupIdByCode(string code)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT GroupID FROM `group` WHERE InviteCode = '" + code + "' LIMIT 1";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				try
				{
					connection.Open();
					object result = cmd.ExecuteScalar();
					if (result != null)
					{
						return Convert.ToInt32(result);
					}
					return 0;
				}
				catch (Exception ex)
				{
					throw new Exception("GetGroupIdByCode failed: " + ex.Message);
				}
			}
		}

		public List<Transaction> GetTransactionsByGroup(int groupId)
		{
			List<Transaction> transactions = new List<Transaction>();
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT * FROM `transaction` WHERE GroupID = " + groupId + " ORDER BY Date DESC";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				try
				{
					connection.Open();
					MySqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						Transaction t = new Transaction();
						t.TransactionID = Convert.ToInt32(reader["TransactionID"]);
						t.UserID = Convert.ToInt32(reader["UserID"]);
						t.GroupID = Convert.ToInt32(reader["GroupID"]);
						t.Amount = Convert.ToDecimal(reader["Amount"]);
						t.Description = reader["Description"].ToString();
						t.Date = Convert.ToDateTime(reader["Date"]);
						transactions.Add(t);
					}
				}
				catch (Exception ex)
				{
					throw new Exception("GetTransactionsByGroup failed: " + ex.Message);
				}
			}
			return transactions;
		}

		public void UpdateTransaction(Transaction t)
		{
			string amount = t.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
			string query = "UPDATE `transaction` SET CategoryID = " + t.CategoryID + ", Type = '" + t.Type +
						   "', Amount = " + amount + ", Description = '" + t.Description +
						   "', `Date` = '" + t.Date.ToString("yyyy-MM-dd HH:mm:ss") +
						   "' WHERE TransactionID = " + t.TransactionID;
			this.ExecuteSimple(query);
		}

		public void DeleteTransaction(int transactionId)
		{
			string query = "DELETE FROM `transaction` WHERE TransactionID = " + transactionId;
			this.ExecuteSimple(query);
		}

		public void AttachReceiptToTransaction(int transactionId, int receiptId)
		{
			string query = "UPDATE `transaction` SET ReceiptID = " + receiptId + " WHERE TransactionID = " + transactionId;
			this.ExecuteSimple(query);
		}

		public void UpdateTransactionCategory(int transactionId, int categoryId)
		{
			string query = "UPDATE `transaction` SET CategoryID = " + categoryId + " WHERE TransactionID = " + transactionId;
			this.ExecuteSimple(query);
		}

		public int InsertCategory(Category cat)
		{
			int defaultVal;
			if (cat.IsDefault == true)
			{
				defaultVal = 1;
			}
			else
			{
				defaultVal = 0;
			}

			string query = "INSERT INTO category(UserID, Name, Type, IsDefault) VALUES(" +
						   cat.UserID + ", '" + cat.Name + "', '" + cat.Type + "', " + defaultVal + ");";
			return this.Insert(query);
		}

		public void UpdateCategory(int categoryId, string newName)
		{
			string query = "UPDATE category SET Name = '" + newName + "' WHERE CategoryID = " + categoryId;
			this.ExecuteSimple(query);
		}

		public void DeleteCategory(int categoryId)
		{
			string query = "DELETE FROM category WHERE CategoryID = " + categoryId;
			this.ExecuteSimple(query);
		}

		public List<Category> GetDefaultCategories()
		{
			List<Category> categories = new List<Category>();
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT * FROM category WHERE IsDefault = 1";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				try
				{
					connection.Open();
					MySqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						Category c = new Category();
						c.CategoryID = Convert.ToInt32(reader["CategoryID"]);
						c.UserID = Convert.ToInt32(reader["UserID"]);
						c.Name = reader["Name"].ToString();
						c.Type = reader["Type"].ToString();
						c.IsDefault = Convert.ToInt32(reader["IsDefault"]) == 1;
						categories.Add(c);
					}
				}
				catch (Exception ex)
				{
					throw new Exception("GetDefaultCategories failed: " + ex.Message);
				}
			}
			return categories;
		}

		public void DeleteProfile(int userId)
		{
			string query = "DELETE FROM user_profile WHERE UserID = " + userId;
			this.ExecuteSimple(query);
		}

		public void UpdateVerificationStatus(int userId, bool isVerified)
		{
			int val;
			if (isVerified == true)
			{
				val = 1;
			}
			else
			{
				val = 0;
			}
			string query = "UPDATE user SET IsVerified = " + val + " WHERE UserID = " + userId;
			this.ExecuteSimple(query);
		}

		public bool GetVerificationStatus(int userId)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT IsVerified FROM user WHERE UserID = " + userId;
				MySqlCommand cmd = new MySqlCommand(query, connection);
				try
				{
					connection.Open();
					return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
				}
				catch (Exception ex)
				{
					throw new Exception("GetVerificationStatus failed: " + ex.Message);
				}
			}
		}

		public string GetResetToken(string email)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT ResetToken FROM user WHERE Email = '" + email + "' LIMIT 1";
				MySqlCommand cmd = new MySqlCommand(query, connection);

				try
				{
					connection.Open();
					object result = cmd.ExecuteScalar();

					if (result != null)
					{
						return result.ToString();
					}
					else
					{
						return null;
					}
				}
				catch (Exception ex)
				{
					throw new Exception("GetResetToken failed: " + ex.Message);
				}
			}
		}

		public int InsertBudget(Budget budget)
		{
			string limitAmount = budget.LimitAmount.ToString(System.Globalization.CultureInfo.InvariantCulture);
			string query = "INSERT INTO budget(UserID, CategoryID, LimitAmount, Month) VALUES(" +
						   budget.UserId + ", " + budget.CategoryId + ", " + limitAmount + ", '" + budget.Month + "');";
			return this.Insert(query);
		}

		public void UpdateBudget(Budget budget)
		{
			string limitAmount = budget.LimitAmount.ToString(System.Globalization.CultureInfo.InvariantCulture);
			string query = "UPDATE budget SET CategoryID = " + budget.CategoryId +
				", LimitAmount = " + limitAmount +
				", Month = '" + budget.Month +
				"' WHERE BudgetID = " + budget.BudgetId;
			this.ExecuteSimple(query);
		}

		public void DeleteBudget(int budgetId)
		{
			string query = "DELETE FROM budget WHERE BudgetID = " + budgetId;
			this.ExecuteSimple(query);
		}

		public decimal GetSpentAmount(int userId, int categoryId, string month)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT SUM(Amount) FROM `transaction` WHERE UserID = " + userId +
					" AND CategoryID = " + categoryId +
					" AND Type = 'Expense' " +
					" AND DATE_FORMAT(`Date`, '%Y-%m') = '" + month + "'";

				MySqlCommand cmd = new MySqlCommand(query, connection);

				try
				{
					connection.Open();
					object result = cmd.ExecuteScalar();

					if (result == DBNull.Value || result == null)
					{
						return 0;
					}
					return Convert.ToDecimal(result);
				}
				catch (Exception ex)
				{
					throw new Exception("GetSpentAmount failed: " + ex.Message);
				}
			}
		}

		public int InsertRecurringTransaction(RecurringTransaction rt)
		{
			string amount = rt.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
			int active;
			if (rt.IsActive == true)
			{
				active = 1;
			}
			else
			{
				active = 0;
			}

			string query = "INSERT INTO recurring_transaction (AccountID, CategoryID, Type, Amount, Frequency, StartDate, NextRunDate, IsActive) " +
						   "VALUES (" +
						   rt.AccountId + ", " +
						   rt.CategoryId + ", '" +
						   rt.Type + "', " +
						   amount + ", '" +
						   rt.Frequency + "', '" +
						   rt.StartDate.ToString("yyyy-MM-dd HH:mm:ss") + "', '" +
						   rt.NextRunDate.ToString("yyyy-MM-dd HH:mm:ss") + "', " +
						   active + ");";
			return this.Insert(query);
		}

		public void UpdateRecurringTransaction(RecurringTransaction rt)
		{
			string amount = rt.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
			int active;
			if (rt.IsActive == true)
			{
				active = 1;
			}
			else
			{
				active = 0;
			}

			string query = "UPDATE recurring_transaction SET " +
						   "CategoryID = " + rt.CategoryId + ", " +
						   "Type = '" + rt.Type + "', " +
						   "Amount = " + amount + ", " +
						   "Frequency = '" + rt.Frequency + "', " +
						   "StartDate = '" + rt.StartDate.ToString("yyyy-MM-dd HH:mm:ss") + "', " +
						   "NextRunDate = '" + rt.NextRunDate.ToString("yyyy-MM-dd HH:mm:ss") + "', " +
						   "IsActive = " + active +
						   " WHERE RecurringID = " + rt.RecurringId;
			this.ExecuteSimple(query);
		}

		public void DeleteRecurringTransaction(int recurringId)
		{
			string query = "DELETE FROM recurring_transaction WHERE RecurringID = " + recurringId;
			this.ExecuteSimple(query);
		}

		public int InsertReceipt(Receipt receipt)
		{
			string query = "INSERT INTO receipt(TransactionID, FilePath, FileType, UploadedAt) VALUES(" +
						   receipt.TransactionID + ", '" +
						   receipt.FilePath + "', '" +
						   receipt.FileType + "', '" +
						   DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "');";
			return this.Insert(query);
		}

		public void DeleteReceipt(int receiptId)
		{
			string query = "DELETE FROM receipt WHERE ReceiptID = " + receiptId;
			this.ExecuteSimple(query);
		}

		public void UpdateGroup(Group group)
		{
			string query = "UPDATE `group` SET Name = '" + group.Name +
						   "' WHERE GroupID = " + group.GroupID;

			this.ExecuteSimple(query);
		}

		public void DeleteGroup(int groupId)
		{
			string query = "DELETE FROM `group` WHERE GroupID = " + groupId;
			this.ExecuteSimple(query);
		}

		public Group GetGroupById(int groupId)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT * FROM `group` WHERE GroupID = " + groupId;
				MySqlCommand cmd = new MySqlCommand(query, connection);

				try
				{
					connection.Open();
					MySqlDataReader reader = cmd.ExecuteReader();

					if (reader.Read())
					{
						Group group = new Group();
						group.GroupID = Convert.ToInt32(reader["GroupID"]);
						group.CreatedByUserID = Convert.ToInt32(reader["CreatedByUserID"]);
						group.Name = reader["Name"].ToString();
						group.Description = reader["Description"].ToString();
						group.InviteCode = reader["InviteCode"].ToString();
						group.CreatedAt = Convert.ToDateTime(reader["CreatedAt"]);
						return group;
					}

					return null;
				}
				catch (Exception ex)
				{
					throw new Exception("GetGroupById failed: " + ex.Message);
				}
			}
		}

		public void InsertGroupMember(GroupMember member)
		{
			string query = "INSERT INTO group_member(GroupID, UserID, JoinedAt) VALUES(" +
						   member.GroupID + ", " +
						   member.UserID + ", '" +
						   DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "');";

			this.ExecuteSimple(query);
		}

		public void DeleteGroupMember(int groupId, int userId)
		{
			string query = "DELETE FROM group_member WHERE GroupID = " + groupId +
						   " AND UserID = " + userId;

			this.ExecuteSimple(query);
		}

		public List<GroupMember> GetGroupMembers(int groupId)
		{
			List<GroupMember> members = new List<GroupMember>();

			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT * FROM group_member WHERE GroupID = " + groupId;
				MySqlCommand cmd = new MySqlCommand(query, connection);

				try
				{
					connection.Open();
					MySqlDataReader reader = cmd.ExecuteReader();

					while (reader.Read())
					{
						GroupMember member = new GroupMember();
						member.GroupID = Convert.ToInt32(reader["GroupID"]);
						member.UserID = Convert.ToInt32(reader["UserID"]);
						member.JoinedAt = Convert.ToDateTime(reader["JoinedAt"]);

						members.Add(member);
					}
				}
				catch (Exception ex)
				{
					throw new Exception("GetGroupMembers failed: " + ex.Message);
				}
			}

			return members;
		}

		public User GetUserById(int userId)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT * FROM user WHERE UserID = " + userId;
				MySqlCommand cmd = new MySqlCommand(query, connection);

				try
				{
					connection.Open();
					MySqlDataReader reader = cmd.ExecuteReader();

					if (reader.Read())
					{
						User user = new User();
						user.UserID = Convert.ToInt32(reader["UserID"]);
						user.Username = reader["Username"].ToString();
						user.Email = reader["Email"].ToString();
						if (reader["Role"].ToString() == "Admin")
						{
							user.Role = UserRole.Admin;
						}
						else
						{
							user.Role = UserRole.User;
						}
						user.IsActive = Convert.ToInt32(reader["IsActive"]) == 1;
						user.CreatedAt = Convert.ToDateTime(reader["CreatedAt"]);

						return user;
					}

					return null;
				}
				catch (Exception ex)
				{
					throw new Exception("GetUserById failed: " + ex.Message);
				}
			}
		}

		public bool ResendVerification(string email, string username)
		{
			string newCode = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();

			try
			{
				string query = "UPDATE user SET ResetToken = '" + newCode + "' WHERE Email = '" + email + "'";
				this.ExecuteSimple(query);

				EmailService mail = new EmailService();
				return mail.SendVerificationCode(email, username, newCode);
			}
			catch
			{
				return false;
			}
		}

		public bool ResendPasswordReset(string email, string username)
		{
			string newCode = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();

			try
			{
				string query = "UPDATE user SET ResetToken = '" + newCode + "' WHERE Email = '" + email + "'";
				this.ExecuteSimple(query);

				EmailService mail = new EmailService();
				return mail.SendResetToken(email, username, newCode);
			}
			catch
			{
				return false;
			}
		}

		public bool EmailExists(string email)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT COUNT(*) FROM user WHERE Email = '" + email + "'";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				try
				{
					connection.Open();
					int count = Convert.ToInt32(cmd.ExecuteScalar());

					return count > 0;
				}
				catch (Exception ex)
				{
					throw new Exception("EmailExists check failed: " + ex.Message);
				}
			}
		}

		public User GetUserByEmail(string email)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT * FROM user WHERE Email = @email LIMIT 1";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@email", email);
				try
				{
					connection.Open();
					using (MySqlDataReader reader = cmd.ExecuteReader())
					{
						if (reader.Read())
						{
							User u = new User();
							u.UserID = Convert.ToInt32(reader["UserID"]);
							u.Username = reader["Username"].ToString();
							u.Email = reader["Email"].ToString();
							u.Role = reader["Role"].ToString() == "Admin" ? UserRole.Admin : UserRole.User;
							u.IsActive = Convert.ToInt32(reader["IsActive"]) == 1;
							return u;
						}
					}
					return null;
				}
				catch (Exception ex) { throw new Exception("GetUserByEmail failed: " + ex.Message); }
			}
		}

		public UserProfile GetProfileByUserId(int userId)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT * FROM user_profile WHERE UserID = @userId LIMIT 1";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@userId", userId);
				try
				{
					connection.Open();
					using (MySqlDataReader reader = cmd.ExecuteReader())
					{
						if (reader.Read())
						{
							return new UserProfile
							{
								ProfileID = Convert.ToInt32(reader["ProfileID"]),
								UserID = Convert.ToInt32(reader["UserID"]),
								FirstName = reader["FirstName"].ToString(),
								LastName = reader["LastName"].ToString(),
								PhoneNumber = reader["PhoneNumber"].ToString(),
								AvatarUrl = reader["AvatarURL"].ToString(),
								PreferredCurrency = reader["PreferedCurrency"].ToString()
							};
						}
					}
					return null;
				}
				catch (Exception ex) { throw new Exception("GetProfileByUserId failed: " + ex.Message); }
			}
		}

		public List<Category> GetCategoriesByUser(int userId)
		{
			List<Category> categories = new List<Category>();
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				// Fetches system defaults (UserID is null/0) AND user-specific categories
				string query = "SELECT * FROM category WHERE UserID = @userId OR IsDefault = 1";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@userId", userId);
				try
				{
					connection.Open();
					MySqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						categories.Add(new Category
						{
							CategoryID = Convert.ToInt32(reader["CategoryID"]),
							UserID = reader["UserID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["UserID"]),
							Name = reader["Name"].ToString(),
							Type = reader["Type"].ToString(),
							IsDefault = Convert.ToInt32(reader["IsDefault"]) == 1
						});
					}
				}
				catch (Exception ex) { throw new Exception("GetCategoriesByUser failed: " + ex.Message); }
			}
			return categories;
		}

		public List<Account> GetAccountsByUser(int userId)
		{
			List<Account> accounts = new List<Account>();
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT * FROM account WHERE UserID = @userId";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@userId", userId);
				try
				{
					connection.Open();
					MySqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						accounts.Add(new Account
						{
							AccountID = Convert.ToInt32(reader["AccountID"]),
							UserID = Convert.ToInt32(reader["UserID"]),
							Name = reader["Name"].ToString(),
							AccountType = reader["AccountType"].ToString(),
							Balance = Convert.ToDecimal(reader["Balance"]),
							Currency = reader["Currency"].ToString()
						});
					}
				}
				catch (Exception ex) { throw new Exception("GetAccountsByUser failed: " + ex.Message); }
			}
			return accounts;
		}
	}
}