using System;
using System.Collections.Generic;
using MySqlConnector;

namespace FinancyApplication
{
	public class Data
	{
		private string connectionString = "datasource=127.0.0.1;port=3306;username=root;password=;database=expense_tracker;";

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
					Console.WriteLine("Insert error: " + ex.Message);
					return -1;
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
					Console.WriteLine("GetUserCount error: " + ex.Message);
					return 0;
				}
			}
		}

		public int InsertUser(User user, string password)
		{
			string query = "INSERT INTO user(Username, Email, Password, Role, CreatedAt, IsActive) VALUES('" +
						   user.Username + "', '" + user.Email + "', '" + password + "', '" +
						   user.Role + "', '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', 1);";
			return this.Insert(query);
		}

		// Returns the stored BCrypt hash for a given email so we can verify it in User.Login()
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
					return result != null ? result.ToString() : null;
				}
				catch (Exception ex)
				{
					Console.WriteLine("GetPasswordHash error: " + ex.Message);
					return null;
				}
			}
		}

		// Rewrote to use BCrypt — fetches the stored hash and verifies against it
		public bool ValidateLogin(string email, string password)
		{
			string storedHash = GetPasswordHash(email);
			if (string.IsNullOrEmpty(storedHash)) return false;

			try
			{
				return BCrypt.Net.BCrypt.Verify(password, storedHash);
			}
			catch (Exception ex)
			{
				Console.WriteLine("ValidateLogin error: " + ex.Message);
				return false;
			}
		}

		public void UpdateResetToken(string email, string token)
		{
			string query = "UPDATE user SET ResetToken = '" + token + "' WHERE Email = '" + email + "'";
			this.ExecuteSimple(query);
		}

		public void UpdateUserStatus(int userId, bool isActive)
		{
			int status = isActive ? 1 : 0;
			string query = "UPDATE user SET IsActive = " + status + " WHERE UserID = " + userId;
			this.ExecuteSimple(query);
		}

		public void UpdateUserRole(int userId, string role)
		{
			string query = "UPDATE user SET Role = '" + role + "' WHERE UserID = " + userId;
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
						transactions.Add(new Transaction
						{
							TransactionID = Convert.ToInt32(reader["TransactionID"]),
							UserID = Convert.ToInt32(reader["UserID"]),
							AccountID = Convert.ToInt32(reader["AccountID"]),
							CategoryID = Convert.ToInt32(reader["CategoryID"]),
							Type = reader["Type"].ToString(),
							Amount = Convert.ToDecimal(reader["Amount"]),
							Description = reader["Description"].ToString(),
							Date = Convert.ToDateTime(reader["Date"])
						});
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("GetTransactionsByAccount error: " + ex.Message);
				}
			}
			return transactions;
		}

		public int InsertCategory(Category cat)
		{
			int defaultVal = cat.IsDefault ? 1 : 0;
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
						transactions.Add(new Transaction
						{
							TransactionID = Convert.ToInt32(reader["TransactionID"]),
							UserID = Convert.ToInt32(reader["UserID"]),
							AccountID = Convert.ToInt32(reader["AccountID"]),
							CategoryID = Convert.ToInt32(reader["CategoryID"]),
							Type = reader["Type"].ToString(),
							Amount = Convert.ToDecimal(reader["Amount"]),
							Description = reader["Description"].ToString(),
							Date = Convert.ToDateTime(reader["Date"])
						});
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("GetTransactionsByCategory error: " + ex.Message);
				}
			}
			return transactions;
		}

		public int InsertTransaction(Transaction t)
		{
			string amount = t.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
			string query = "INSERT INTO `transaction` (UserID, AccountID, CategoryID, Type, Amount, Description, `Date`) " +
						   "VALUES (" + t.UserID + ", " + t.AccountID + ", " + t.CategoryID + ", '" + t.Type + "', " +
						   amount + ", '" + t.Description + "', '" +
						   t.Date.ToString("yyyy-MM-dd HH:mm:ss") + "');";
			return this.Insert(query);
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
					Console.WriteLine("GetSpentAmount error: " + ex.Message);
					return 0;
				}
			}
		}

		public int InsertRecurringTransaction(RecurringTransaction rt)
        {
			string amount = rt.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
			int active = rt.IsActive ? 1 : 0;

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
			int active = rt.IsActive ? 1 : 0;

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
						   receipt.FileType +  "', '" +
						   DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "');";
			return this.Insert(query);
		}

		public void DeleteReceipt(int receiptId)
        {
			string query = "DELETE FROM receipt WHERE ReceiptID = " + receiptId;
			this.ExecuteSimple(query);
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
					Console.WriteLine("ExecuteSimple error: " + ex.Message);
				}
			}
		}
	}
}