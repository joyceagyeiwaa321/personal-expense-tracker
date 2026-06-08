using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;

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

		public void ExecuteSimple(string query)
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
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "INSERT INTO user(Username, Email, Password, Role, CreatedAt, IsActive) VALUES(@username, @email, @password, @role, @createdAt, 1)";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@username", user.Username);
				cmd.Parameters.AddWithValue("@email", user.Email);
				cmd.Parameters.AddWithValue("@password", password);
				cmd.Parameters.AddWithValue("@role", user.Role.ToString());
				cmd.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
				try
				{
					connection.Open();
					cmd.ExecuteNonQuery();
					return (int)cmd.LastInsertedId;
				}
				catch (Exception ex)
				{
					throw new Exception("InsertUser failed: " + ex.Message);
				}
			}
		}

		public string GetPasswordHash(string email)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT Password FROM user WHERE Email = @email LIMIT 1";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@email", email);
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
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "UPDATE user SET ResetToken = @token WHERE Email = @email";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@token", token);
				cmd.Parameters.AddWithValue("@email", email);
				try
				{
					connection.Open();
					cmd.ExecuteNonQuery();
				}
				catch (Exception ex)
				{
					throw new Exception("UpdateResetToken failed: " + ex.Message);
				}
			}
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
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "UPDATE user SET Role = @role WHERE UserID = " + userId;
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@role", role);
				try
				{
					connection.Open();
					cmd.ExecuteNonQuery();
				}
				catch (Exception ex)
				{
					throw new Exception("UpdateUserRole failed: " + ex.Message);
				}
			}
		}

		public void UpdateUserPassword(int userId, string newHashedPassword)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "UPDATE user SET Password = @password WHERE UserID = " + userId;
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@password", newHashedPassword);
				try
				{
					connection.Open();
					cmd.ExecuteNonQuery();
				}
				catch (Exception ex)
				{
					throw new Exception("UpdateUserPassword failed: " + ex.Message);
				}
			}
		}

		public void UpdateUsername(int userId, string newUsername)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "UPDATE user SET Username = @username WHERE UserID = " + userId;
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@username", newUsername);
				try
				{
					connection.Open();
					cmd.ExecuteNonQuery();
				}
				catch (Exception ex)
				{
					throw new Exception("UpdateUsername failed: " + ex.Message);
				}
			}
		}
		public void DeleteUser(int userId)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				try
				{
					connection.Open();

					string[] queries = new string[]
					{
						// clean up groups this user created (children first, then the group itself)
						"DELETE FROM expense_split WHERE TransactionID IN (SELECT TransactionID FROM `transaction` WHERE GroupID IN (SELECT GroupID FROM `group` WHERE CreatedByUserID = " + userId + "))",
						"DELETE FROM receipt WHERE TransactionID IN (SELECT TransactionID FROM `transaction` WHERE GroupID IN (SELECT GroupID FROM `group` WHERE CreatedByUserID = " + userId + "))",
						"DELETE FROM `transaction` WHERE GroupID IN (SELECT GroupID FROM `group` WHERE CreatedByUserID = " + userId + ")",
						"DELETE FROM group_member WHERE GroupID IN (SELECT GroupID FROM `group` WHERE CreatedByUserID = " + userId + ")",
						"DELETE FROM `group` WHERE CreatedByUserID = " + userId,
						// clean up this user's memberships and splits in other groups
						"DELETE FROM expense_split WHERE UserID = " + userId,
						"DELETE FROM group_member WHERE UserID = " + userId,
						// clean up user's own data
						"DELETE FROM receipt WHERE TransactionID IN (SELECT TransactionID FROM `transaction` WHERE UserID = " + userId + ")",
						"DELETE FROM `transaction` WHERE UserID = " + userId,
						"DELETE FROM recurring_transaction WHERE AccountID IN (SELECT AccountID FROM account WHERE UserID = " + userId + ")",
						"DELETE FROM budget WHERE UserID = " + userId,
						"DELETE FROM goal WHERE UserID = " + userId,
						"DELETE FROM account WHERE UserID = " + userId,
						"DELETE FROM category WHERE UserID = " + userId,
						"DELETE FROM user_profile WHERE UserID = " + userId,
						"DELETE FROM user WHERE UserID = " + userId
					};

					foreach (string q in queries)
					{
						MySqlCommand cmd = new MySqlCommand(q, connection);
						cmd.ExecuteNonQuery();
					}
				}
				catch (Exception ex)
				{
					throw new Exception("Delete failed: " + ex.Message);
				}
			}
		}

		public void UpdateProfile(UserProfile profile)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "UPDATE user_profile SET FirstName=@firstName, LastName=@lastName, PhoneNumber=@phone, AvatarURL=@avatar, PreferedCurrency=@currency, NotifGoalReminders=@notifGoal WHERE UserID = " + profile.UserID;
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@firstName", profile.FirstName);
				cmd.Parameters.AddWithValue("@lastName", profile.LastName);
				cmd.Parameters.AddWithValue("@phone", profile.PhoneNumber);
				cmd.Parameters.AddWithValue("@avatar", profile.AvatarUrl);
				cmd.Parameters.AddWithValue("@currency", profile.PreferredCurrency);
				cmd.Parameters.AddWithValue("@notifGoal", profile.NotifGoalReminders ? 1 : 0);
				try
				{
					connection.Open();
					cmd.ExecuteNonQuery();
				}
				catch (Exception ex)
				{
					throw new Exception("UpdateProfile failed: " + ex.Message);
				}
			}
		}

		public int InsertProfile(UserProfile profile)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "INSERT INTO user_profile(UserID, FirstName, LastName, PhoneNumber, AvatarURL, PreferedCurrency, NotifGoalReminders) VALUES(@userId, @firstName, @lastName, @phone, @avatar, @currency, 1)";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@userId", profile.UserID);
				cmd.Parameters.AddWithValue("@firstName", profile.FirstName);
				cmd.Parameters.AddWithValue("@lastName", profile.LastName);
				cmd.Parameters.AddWithValue("@phone", profile.PhoneNumber);
				cmd.Parameters.AddWithValue("@avatar", profile.AvatarUrl);
				cmd.Parameters.AddWithValue("@currency", profile.PreferredCurrency);
				try
				{
					connection.Open();
					cmd.ExecuteNonQuery();
					return (int)cmd.LastInsertedId;
				}
				catch (Exception ex)
				{
					throw new Exception("InsertProfile failed: " + ex.Message);
				}
			}
		}

		public int InsertAccount(Account acc)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "INSERT INTO account(UserID, Name, AccountType, Balance, Currency, CreatedAt) VALUES(@userId, @name, @type, @balance, @currency, @createdAt)";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@userId", acc.UserID);
				cmd.Parameters.AddWithValue("@name", acc.Name);
				cmd.Parameters.AddWithValue("@type", acc.AccountType);
				cmd.Parameters.AddWithValue("@balance", acc.Balance.ToString(System.Globalization.CultureInfo.InvariantCulture));
				cmd.Parameters.AddWithValue("@currency", acc.Currency);
				cmd.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
				try
				{
					connection.Open();
					cmd.ExecuteNonQuery();
					return (int)cmd.LastInsertedId;
				}
				catch (Exception ex)
				{
					throw new Exception("InsertAccount failed: " + ex.Message);
				}
			}
		}

		public void UpdateAccountBalance(int accountId, decimal amount)
		{
			string amt = amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
			string query = "UPDATE account SET Balance = Balance + " + amt + " WHERE AccountID = " + accountId;
			this.ExecuteSimple(query);
		}

		public void RenameAccount(int accountId, string newName)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "UPDATE account SET Name = @name WHERE AccountID = " + accountId;
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@name", newName);
				try
				{
					connection.Open();
					cmd.ExecuteNonQuery();
				}
				catch (Exception ex)
				{
					throw new Exception("RenameAccount failed: " + ex.Message);
				}
			}
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

		public List<Transaction> GetTransactionsByUser(int userId)
		{
			List<Transaction> transactions = new List<Transaction>();
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT * FROM `transaction` WHERE UserID = " + userId + " ORDER BY `Date` DESC";
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
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string gIDValue = "NULL";
				if (t.GroupID > 0)
				{
					gIDValue = t.GroupID.ToString();
				}

				string query = "INSERT INTO `transaction` (UserID, AccountID, CategoryID, GroupID, Type, Amount, Description, `Date`) " +
							   "VALUES (" + t.UserID + ", " + t.AccountID + ", " + t.CategoryID + ", " + gIDValue + ", @type, " +
							   t.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", @description, '" +
							   t.Date.ToString("yyyy-MM-dd HH:mm:ss") + "')";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@type", t.Type);
				cmd.Parameters.AddWithValue("@description", t.Description);
				try
				{
					connection.Open();
					cmd.ExecuteNonQuery();
					return (int)cmd.LastInsertedId;
				}
				catch (Exception ex)
				{
					throw new Exception("InsertTransaction failed: " + ex.Message);
				}
			}
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
				string query = @"
                    SELECT t.*, c.Name AS CategoryName 
                    FROM `transaction` t
                    LEFT JOIN category c ON t.CategoryID = c.CategoryID
                    WHERE t.GroupID = " + groupId + " ORDER BY t.Date DESC";
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
						t.CategoryID = Convert.ToInt32(reader["CategoryID"]);
						t.CategoryName = reader["CategoryName"]?.ToString() ?? "—"; // ADD THIS
						t.Amount = Convert.ToDecimal(reader["Amount"]);
						t.Description = reader["Description"].ToString();
						t.Date = Convert.ToDateTime(reader["Date"]);
						t.Type = reader["Type"]?.ToString() ?? "Expense";
						t.Status = reader["Status"]?.ToString() ?? "Pending";
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
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "UPDATE `transaction` SET CategoryID = " + t.CategoryID + ", Type = @type" +
							   ", Amount = " + t.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", Description = @description" +
							   ", `Date` = '" + t.Date.ToString("yyyy-MM-dd HH:mm:ss") +
							   "' WHERE TransactionID = " + t.TransactionID;
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@type", t.Type);
				cmd.Parameters.AddWithValue("@description", t.Description);
				try
				{
					connection.Open();
					cmd.ExecuteNonQuery();
				}
				catch (Exception ex)
				{
					throw new Exception("UpdateTransaction failed: " + ex.Message);
				}
			}
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

			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "INSERT INTO category(UserID, Name, Type, IsDefault) VALUES(" + cat.UserID + ", @name, @type, " + defaultVal + ")";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@name", cat.Name);
				cmd.Parameters.AddWithValue("@type", cat.Type);
				try
				{
					connection.Open();
					cmd.ExecuteNonQuery();
					return (int)cmd.LastInsertedId;
				}
				catch (Exception ex)
				{
					throw new Exception("InsertCategory failed: " + ex.Message);
				}
			}
		}

		public void UpdateCategory(int categoryId, string newName)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "UPDATE category SET Name = @name WHERE CategoryID = " + categoryId;
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@name", newName);
				try
				{
					connection.Open();
					cmd.ExecuteNonQuery();
				}
				catch (Exception ex)
				{
					throw new Exception("UpdateCategory failed: " + ex.Message);
				}
			}
		}

		// Admin: update ALL default rows sharing the same original name+type
		public void UpdateAllDefaultCategories(string oldName, string oldType, string newName, string newType)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "UPDATE category SET Name = @newName, Type = @newType WHERE IsDefault = 1 AND Name = @oldName AND Type = @type";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@newName", newName);
				cmd.Parameters.AddWithValue("@newType", newType);
				cmd.Parameters.AddWithValue("@oldName", oldName);
				cmd.Parameters.AddWithValue("@type", oldType);
				try
				{
					connection.Open();
					cmd.ExecuteNonQuery();
				}
				catch (Exception ex)
				{
					throw new Exception("UpdateAllDefaultCategories failed: " + ex.Message);
				}
			}
		}

		// Admin: delete ALL default rows sharing the same name+type
		public void DeleteAllDefaultCategories(string name, string type)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "DELETE FROM category WHERE IsDefault = 1 AND Name = @name AND Type = @type";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@name", name);
				cmd.Parameters.AddWithValue("@type", type);
				try
				{
					connection.Open();
					cmd.ExecuteNonQuery();
				}
				catch (Exception ex)
				{
					throw new Exception("DeleteAllDefaultCategories failed: " + ex.Message);
				}
			}
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
		public string GetResetToken(string email)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT ResetToken FROM user WHERE Email = @email LIMIT 1";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@email", email);

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

		public List<Budget> GetBudgetsByUser(int userId, string month)
		{
			List<Budget> budgets = new List<Budget>();
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				// month is stored as "yyyy-MM" string in the budget table
				string query = "SELECT * FROM budget WHERE UserID = @userId AND Month = @month";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@userId", userId);
				cmd.Parameters.AddWithValue("@month", month);
				try
				{
					connection.Open();
					MySqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						budgets.Add(new Budget
						{
							BudgetId = Convert.ToInt32(reader["BudgetID"]),
							UserId = Convert.ToInt32(reader["UserID"]),
							CategoryId = Convert.ToInt32(reader["CategoryID"]),
							LimitAmount = Convert.ToDecimal(reader["LimitAmount"]),
							Month = reader["Month"].ToString()
						});
					}
				}
				catch (Exception ex)
				{
					throw new Exception("GetBudgetsByUser failed: " + ex.Message);
				}
			}
			return budgets;
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

		public List<RecurringTransaction> GetRecurringByUser(int userId)
		{
			List<RecurringTransaction> list = new List<RecurringTransaction>();
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT rt.* FROM recurring_transaction rt " +
							   "INNER JOIN account a ON rt.AccountID = a.AccountID " +
							   "WHERE a.UserID = @userId " +
							   "ORDER BY rt.NextRunDate ASC";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@userId", userId);
				try
				{
					connection.Open();
					MySqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						list.Add(new RecurringTransaction
						{
							RecurringId = Convert.ToInt32(reader["RecurringID"]),
							AccountId = Convert.ToInt32(reader["AccountID"]),
							CategoryId = Convert.ToInt32(reader["CategoryID"]),
							Type = reader["Type"].ToString(),
							Amount = Convert.ToDecimal(reader["Amount"]),
							Frequency = reader["Frequency"].ToString(),
							StartDate = Convert.ToDateTime(reader["StartDate"]),
							NextRunDate = Convert.ToDateTime(reader["NextRunDate"]),
							IsActive = Convert.ToInt32(reader["IsActive"]) == 1
						});
					}
				}
				catch (Exception ex)
				{
					throw new Exception("GetRecurringByUser failed: " + ex.Message);
				}
			}
			return list;
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
		public bool ResendPasswordReset(string email, string username)
		{
			string newCode = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();

			try
			{
				using (MySqlConnection connection = new MySqlConnection(connectionString))
				{
					string query = "UPDATE user SET ResetToken = @token WHERE Email = @email";
					MySqlCommand cmd = new MySqlCommand(query, connection);
					cmd.Parameters.AddWithValue("@token", newCode);
					cmd.Parameters.AddWithValue("@email", email);
					connection.Open();
					cmd.ExecuteNonQuery();
				}

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
				string query = "SELECT COUNT(*) FROM user WHERE Email = @email";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@email", email);
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
							if (reader["Role"].ToString() == "Admin")
							{
								u.Role = UserRole.Admin;
							}
							else
							{
								u.Role = UserRole.User;
							}
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
								PreferredCurrency = reader["PreferedCurrency"].ToString(),
								NotifGoalReminders = Convert.ToInt32(reader["NotifGoalReminders"]) == 1
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

			// Dedupe by (Name, Type) — the DB has multiple copies of the default
			// categories (a system-default row plus per-user copies seeded at
			// registration). Prefer the row with the lowest CategoryID.
			return categories
				.GroupBy(c => (c.Name.Trim().ToLowerInvariant(), c.Type.Trim().ToLowerInvariant()))
				.Select(g => g.OrderBy(c => c.CategoryID).First())
				.OrderBy(c => c.Type)
				.ThenBy(c => c.Name)
				.ToList();
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

		public List<Group> GetGroupsByUser(int userId)
		{
			List<Group> groups = new List<Group>();
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT g.* FROM `group` g " +
							   "INNER JOIN group_member gm ON g.GroupID = gm.GroupID " +
							   "WHERE gm.UserID = @userId";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@userId", userId);
				try
				{
					connection.Open();
					MySqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						groups.Add(new Group
						{
							GroupID = Convert.ToInt32(reader["GroupID"]),
							CreatedByUserID = Convert.ToInt32(reader["CreatedByUserID"]),
							Name = reader["Name"].ToString(),
							Description = reader["Description"].ToString(),
							InviteCode = reader["InviteCode"].ToString(),
							CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
						});
					}
				}
				catch (Exception ex) { throw new Exception("GetGroupsByUser failed: " + ex.Message); }
			}
			return groups;
		}

		public void CreateGroupInvite(int groupId, int fromUserId, int toUserId)
		{
			using (MySqlConnection con = new MySqlConnection(connectionString))
			{
				MySqlCommand cmd = new MySqlCommand(
					"INSERT INTO groupinvites (GroupID, FromUserID, ToUserID) VALUES (@g, @f, @t)", con);
				cmd.Parameters.AddWithValue("@g", groupId);
				cmd.Parameters.AddWithValue("@f", fromUserId);
				cmd.Parameters.AddWithValue("@t", toUserId);
				try { con.Open(); cmd.ExecuteNonQuery(); }
				catch (Exception ex) { throw new Exception("CreateGroupInvite failed: " + ex.Message); }
			}
		}

		public List<GroupInvite> GetPendingInvites(int toUserId)
		{
			var list = new List<GroupInvite>();
			using (MySqlConnection con = new MySqlConnection(connectionString))
			{
				MySqlCommand cmd = new MySqlCommand(
					"SELECT * FROM groupinvites WHERE ToUserID = @u AND Status = 'Pending'", con);
				cmd.Parameters.AddWithValue("@u", toUserId);
				try
				{
					con.Open();
					MySqlDataReader r = cmd.ExecuteReader();
					while (r.Read())
						list.Add(new GroupInvite
						{
							InviteID = Convert.ToInt32(r["InviteID"]),
							GroupID = Convert.ToInt32(r["GroupID"]),
							FromUserID = Convert.ToInt32(r["FromUserID"]),
							ToUserID = Convert.ToInt32(r["ToUserID"]),
							Status = r["Status"].ToString(),
							SentAt = Convert.ToDateTime(r["SentAt"])
						});
				}
				catch (Exception ex) { throw new Exception("GetPendingInvites failed: " + ex.Message); }
			}
			return list;
		}

		public void RespondToInvite(int inviteId, bool accepted)
		{
			using (MySqlConnection con = new MySqlConnection(connectionString))
			{
				MySqlCommand cmd = new MySqlCommand(
					"UPDATE groupinvites SET Status = @s WHERE InviteID = @i", con);
				cmd.Parameters.AddWithValue("@s", accepted ? "Accepted" : "Declined");
				cmd.Parameters.AddWithValue("@i", inviteId);
				try { con.Open(); cmd.ExecuteNonQuery(); }
				catch (Exception ex) { throw new Exception("RespondToInvite failed: " + ex.Message); }
			}
		}

		public User GetUserByUsername(string username)
		{
			using (MySqlConnection con = new MySqlConnection(connectionString))
			{
				MySqlCommand cmd = new MySqlCommand("SELECT * FROM user WHERE Username = @u LIMIT 1", con);
				cmd.Parameters.AddWithValue("@u", username);
				try
				{
					con.Open();
					MySqlDataReader r = cmd.ExecuteReader();
					if (r.Read())
						return new User
						{
							UserID = Convert.ToInt32(r["UserID"]),
							Username = r["Username"].ToString()
						};
					return null;
				}
				catch (Exception ex) { throw new Exception("GetUserByUsername failed: " + ex.Message); }
			}
		}

		public Dictionary<int, string> GetAllCategoriesRaw()
		{
			var dict = new Dictionary<int, string>();
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT CategoryID, Name FROM category";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				try
				{
					connection.Open();
					MySqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
						dict[Convert.ToInt32(reader["CategoryID"])] = reader["Name"].ToString();
				}
				catch (Exception ex) { throw new Exception("GetAllCategoriesRaw failed: " + ex.Message); }
			}
			return dict;
		}

		public string GetCategoryName(int categoryId)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT Name FROM category WHERE CategoryID = " + categoryId;
				MySqlCommand cmd = new MySqlCommand(query, connection);
				try
				{
					connection.Open();
					object result = cmd.ExecuteScalar();
					return result != null ? result.ToString() : "—";
				}
				catch { return "—"; }
			}
		}

		public void UpdateTransactionStatus(int transactionId, string status)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "UPDATE `transaction` SET Status = @status WHERE TransactionID = " + transactionId;
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@status", status);
				try
				{
					connection.Open();
					cmd.ExecuteNonQuery();
				}
				catch (Exception ex) { throw new Exception("UpdateTransactionStatus failed: " + ex.Message); }
			}
		}

		public void DeleteProfile(int userId)
		{
			string query = "DELETE FROM user_profile WHERE UserID = " + userId;
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

		public bool ResendVerification(string email, string username)
		{
			string newCode = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
			try
			{
				using (MySqlConnection connection = new MySqlConnection(connectionString))
				{
					string query = "UPDATE user SET ResetToken = @token WHERE Email = @email";
					MySqlCommand cmd = new MySqlCommand(query, connection);
					cmd.Parameters.AddWithValue("@token", newCode);
					cmd.Parameters.AddWithValue("@email", email);
					connection.Open();
					cmd.ExecuteNonQuery();
				}
				EmailService mail = new EmailService();
				return mail.SendVerificationCode(email, username, newCode);
			}
			catch { return false; }
		}

		// GOAL CRUD 

		public int InsertGoal(Goal goal)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "INSERT INTO goal(UserID, Name, TargetAmount, SavedAmount, Deadline, CreatedAt) " +
							   "VALUES(@userId, @name, @target, @saved, @deadline, @createdAt);";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@userId", goal.UserId);
				cmd.Parameters.AddWithValue("@name", goal.Name ?? "");
				cmd.Parameters.AddWithValue("@target", goal.TargetAmount);
				cmd.Parameters.AddWithValue("@saved", goal.SavedAmount);
				cmd.Parameters.AddWithValue("@deadline",
					goal.Deadline.HasValue ? (object)goal.Deadline.Value : DBNull.Value);
				cmd.Parameters.AddWithValue("@createdAt", DateTime.Now);
				try
				{
					connection.Open();
					cmd.ExecuteNonQuery();
					return (int)cmd.LastInsertedId;
				}
				catch (Exception ex)
				{
					throw new Exception("InsertGoal failed: " + ex.Message);
				}
			}
		}

		public void UpdateGoal(Goal goal)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "UPDATE goal SET Name = @name, TargetAmount = @target, " +
							   "SavedAmount = @saved, Deadline = @deadline WHERE GoalID = @goalId";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@name", goal.Name ?? "");
				cmd.Parameters.AddWithValue("@target", goal.TargetAmount);
				cmd.Parameters.AddWithValue("@saved", goal.SavedAmount);
				cmd.Parameters.AddWithValue("@deadline",
					goal.Deadline.HasValue ? (object)goal.Deadline.Value : DBNull.Value);
				cmd.Parameters.AddWithValue("@goalId", goal.GoalId);
				try
				{
					connection.Open();
					cmd.ExecuteNonQuery();
				}
				catch (Exception ex)
				{
					throw new Exception("UpdateGoal failed: " + ex.Message);
				}
			}
		}

		public void DeleteGoal(int goalId)
		{
			string query = "DELETE FROM goal WHERE GoalID = " + goalId;
			this.ExecuteSimple(query);
		}

		public List<Goal> GetGoalsByUser(int userId)
		{
			List<Goal> goals = new List<Goal>();
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT * FROM goal WHERE UserID = @userId ORDER BY GoalID DESC";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@userId", userId);
				try
				{
					connection.Open();
					MySqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						Goal g = new Goal
						{
							GoalId = Convert.ToInt32(reader["GoalID"]),
							UserId = Convert.ToInt32(reader["UserID"]),
							Name = reader["Name"].ToString(),
							TargetAmount = Convert.ToDecimal(reader["TargetAmount"]),
							SavedAmount = Convert.ToDecimal(reader["SavedAmount"]),
							Deadline = reader["Deadline"] == DBNull.Value
								? (DateTime?)null
								: Convert.ToDateTime(reader["Deadline"]),
							CreatedAt = reader["CreatedAt"] == DBNull.Value
								? DateTime.MinValue
								: Convert.ToDateTime(reader["CreatedAt"])
						};
						goals.Add(g);
					}
				}
				catch (Exception ex)
				{
					throw new Exception("GetGoalsByUser failed: " + ex.Message);
				}
			}
			return goals;
		}
		// EXPENSE SPLIT CRUD 

		public int InsertExpenseSplit(ExpenseSplit split)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "INSERT INTO expense_split(TransactionID, UserID, Amount, IsPaid, PaidAt) " +
							   "VALUES(@txId, @userId, @amount, @isPaid, @paidAt);";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@txId", split.TransactionID);
				cmd.Parameters.AddWithValue("@userId", split.UserID);
				cmd.Parameters.AddWithValue("@amount", split.Amount);
				cmd.Parameters.AddWithValue("@isPaid", split.IsPaid ? 1 : 0);
				cmd.Parameters.AddWithValue("@paidAt",
					split.PaidAt.HasValue ? (object)split.PaidAt.Value : DBNull.Value);
				try
				{
					connection.Open();
					cmd.ExecuteNonQuery();
					return (int)cmd.LastInsertedId;
				}
				catch (Exception ex)
				{
					throw new Exception("InsertExpenseSplit failed: " + ex.Message);
				}
			}
		}

		public List<ExpenseSplit> GetSplitsByTransaction(int transactionId)
		{
			List<ExpenseSplit> splits = new List<ExpenseSplit>();
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "SELECT * FROM expense_split WHERE TransactionID = @txId";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@txId", transactionId);
				try
				{
					connection.Open();
					MySqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						splits.Add(new ExpenseSplit
						{
							ExpenseSplitID = Convert.ToInt32(reader["ExpenseSplitID"]),
							TransactionID = Convert.ToInt32(reader["TransactionID"]),
							UserID = Convert.ToInt32(reader["UserID"]),
							Amount = Convert.ToDecimal(reader["Amount"]),
							IsPaid = Convert.ToInt32(reader["IsPaid"]) == 1,
							PaidAt = reader["PaidAt"] == DBNull.Value
								? (DateTime?)null
								: Convert.ToDateTime(reader["PaidAt"])
						});
					}
				}
				catch (Exception ex)
				{
					throw new Exception("GetSplitsByTransaction failed: " + ex.Message);
				}
			}
			return splits;
		}

		public void MarkSplitsBetweenUsersPaid(int groupId, int paidByUserId, int oweUserId)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query =
					"UPDATE expense_split es " +
					"INNER JOIN `transaction` t ON es.TransactionID = t.TransactionID " +
					"SET es.IsPaid = 1, es.PaidAt = @now " +
					"WHERE t.GroupID = @groupId " +
					"  AND t.UserID = @paidByUserId " +
					"  AND es.UserID = @oweUserId " +
					"  AND es.IsPaid = 0";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.Parameters.AddWithValue("@groupId", groupId);
				cmd.Parameters.AddWithValue("@paidByUserId", paidByUserId);
				cmd.Parameters.AddWithValue("@oweUserId", oweUserId);
				cmd.Parameters.AddWithValue("@now", DateTime.Now);
				try
				{
					connection.Open();
					cmd.ExecuteNonQuery();
				}
				catch (Exception ex)
				{
					throw new Exception("MarkSplitsBetweenUsersPaid failed: " + ex.Message);
				}
			}
		}

		public void MarkAllGroupExpensesPaid(int groupId)
		{
			using (MySqlConnection connection = new MySqlConnection(connectionString))
			{
				string query = "UPDATE `transaction` SET Status = 'Paid' " +
							   "WHERE GroupID = " + groupId + " AND Status = 'Pending'";
				MySqlCommand cmd = new MySqlCommand(query, connection);
				try
				{
					connection.Open();
					cmd.ExecuteNonQuery();
				}
				catch (Exception ex)
				{
					throw new Exception("MarkAllGroupExpensesPaid failed: " + ex.Message);
				}
			}
		}
	}
}