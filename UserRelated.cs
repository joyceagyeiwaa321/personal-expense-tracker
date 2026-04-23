using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace FinancyApplication
{
	public enum UserRole { User, Admin }

	public class User
	{
		public int UserID { get; set; }
		public string Username { get; set; }
		public string Email { get; set; }
		private string PasswordHash { get; set; }
		public UserRole Role { get; set; }
		public bool IsActive { get; set; }
		public string ResetToken { get; set; }
		public DateTime CreatedAt { get; set; }

		public User()
		{
			this.CreatedAt = DateTime.Now;
			this.IsActive = true;
			this.Role = UserRole.User;
		}

		public void Register(string username, string email, string password)
		{
			using (var conn = DataConnection.GetConnection())
			{
				conn.Open();
				string sql = "INSERT INTO users (username, email, password, role, created_at, is_active) VALUES (@n, @e, @p, @r, @d, @a)";
				using (var cmd = new MySqlCommand(sql, conn))
				{
					cmd.Parameters.AddWithValue("@n", username);
					cmd.Parameters.AddWithValue("@e", email);
					cmd.Parameters.AddWithValue("@p", password);
					cmd.Parameters.AddWithValue("@r", Role.ToString());
					cmd.Parameters.AddWithValue("@d", CreatedAt);
					cmd.Parameters.AddWithValue("@a", IsActive);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public bool Login(string email, string password)
		{
			using (var conn = DataConnection.GetConnection())
			{
				conn.Open();
				string sql = "SELECT COUNT(*) FROM users WHERE email=@e AND password=@p AND is_active=1";
				using (var cmd = new MySqlCommand(sql, conn))
				{
					cmd.Parameters.AddWithValue("@e", email);
					cmd.Parameters.AddWithValue("@p", password);
					return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
				}
			}
		}

		public void Logout()
		{
			this.UserID = 0;
			this.Username = null;
		}

		public void ResetPassword(string email)
		{
			this.ResetToken = Guid.NewGuid().ToString().Substring(0, 8);
			using (var conn = DataConnection.GetConnection())
			{
				conn.Open();
				string sql = "UPDATE users SET reset_token=@t WHERE email=@e";
				using (var cmd = new MySqlCommand(sql, conn))
				{
					cmd.Parameters.AddWithValue("@t", ResetToken);
					cmd.Parameters.AddWithValue("@e", email);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public void UpdateRole(UserRole role)
		{
			this.Role = role;
			using (var conn = DataConnection.GetConnection())
			{
				conn.Open();
				string sql = "UPDATE users SET role=@r WHERE user_id=@id";
				using (var cmd = new MySqlCommand(sql, conn))
				{
					cmd.Parameters.AddWithValue("@r", role.ToString());
					cmd.Parameters.AddWithValue("@id", UserID);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public void Deactivate()
		{
			this.IsActive = false;
			using (var conn = DataConnection.GetConnection())
			{
				conn.Open();
				string sql = "UPDATE users SET is_active=0 WHERE user_id=@id";
				using (var cmd = new MySqlCommand(sql, conn))
				{
					cmd.Parameters.AddWithValue("@id", UserID);
					cmd.ExecuteNonQuery();
				}
			}
		}
	}

	public class Admin : User
	{
		public Admin() : base() { this.Role = UserRole.Admin; }

		public List<User> GetAllUsers()
		{
			List<User> list = new List<User>();
			using (var conn = DataConnection.GetConnection())
			{
				conn.Open();
				string sql = "SELECT * FROM users";
				using (var cmd = new MySqlCommand(sql, conn))
				using (var r = cmd.ExecuteReader())
				{
					while (r.Read())
					{
						list.Add(new User
						{
							UserID = (int)r["user_id"],
							Username = r["username"].ToString(),
							Email = r["email"].ToString()
						});
					}
				}
			}
			return list;
		}

		public void DeactivateUser(int userID)
		{
			using (var conn = DataConnection.GetConnection())
			{
				conn.Open();
				string sql = "UPDATE users SET is_active=0 WHERE user_id=@id";
				using (var cmd = new MySqlCommand(sql, conn))
				{
					cmd.Parameters.AddWithValue("@id", userID);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public void DeleteUser(int userID)
		{
			using (var conn = DataConnection.GetConnection())
			{
				conn.Open();
				string sql = "DELETE FROM users WHERE user_id=@id";
				using (var cmd = new MySqlCommand(sql, conn))
				{
					cmd.Parameters.AddWithValue("@id", userID);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public void ResetUserPassword(int userID)
		{
			using (var conn = DataConnection.GetConnection())
			{
				conn.Open();
				string sql = "UPDATE users SET password='Password123' WHERE user_id=@id";
				using (var cmd = new MySqlCommand(sql, conn))
				{
					cmd.Parameters.AddWithValue("@id", userID);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public void ViewAllTransactions()
		{
			using (var conn = DataConnection.GetConnection())
			{
				conn.Open();
				string sql = "SELECT * FROM transactions";
				using (var cmd = new MySqlCommand(sql, conn))
				{
					cmd.ExecuteReader();
				}
			}
		}

		public void ManageCategories()
		{
			//Not now
		}

		public void GenerateReport()
		{
			//Not now
		}

		public void ManageGroups()
		{
			//Not now
		}
	}
}