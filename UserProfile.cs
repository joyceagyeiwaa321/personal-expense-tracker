using System;
using MySql.Data.MySqlClient;

namespace FinancyApplication
{
	public class UserProfile
	{
		public int ProfileID { get; set; }
		public int UserID { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string PhoneNumber { get; set; }
		public string AvatarUrl { get; set; }
		public string PreferredCurrency { get; set; }

		public void UpdateFields(string fName, string lName, string phone)
		{
			this.FirstName = fName;
			this.LastName = lName;
			this.PhoneNumber = phone;

			using (var conn = DataConnection.GetConnection())
			{
				conn.Open();
				string sql = "UPDATE user_profiles SET first_name=@f, last_name=@l, phone=@p WHERE user_id=@uid";
				using (var cmd = new MySqlCommand(sql, conn))
				{
					cmd.Parameters.AddWithValue("@f", fName);
					cmd.Parameters.AddWithValue("@l", lName);
					cmd.Parameters.AddWithValue("@p", phone);
					cmd.Parameters.AddWithValue("@uid", this.UserID);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public string GetFullName()
		{
			return $"{FirstName} {LastName}";
		}

		public void SetPreferredCurrency(string currency)
		{
			this.PreferredCurrency = currency;
			using (var conn = DataConnection.GetConnection())
			{
				conn.Open();
				string sql = "UPDATE user_profiles SET currency=@c WHERE user_id=@uid";
				using (var cmd = new MySqlCommand(sql, conn))
				{
					cmd.Parameters.AddWithValue("@c", currency);
					cmd.Parameters.AddWithValue("@uid", this.UserID);
					cmd.ExecuteNonQuery();
				}
			}
		}
	}
}