using MySql.Data.MySqlClient;
using System;

namespace FinancyApplication
{
	public static class DataConnection
	{
		private static string connectionString = "Server=localhost;Database=expense_tracker;Uid=root;Pwd=;";

		public static MySqlConnection GetConnection()
		{
			return new MySqlConnection(connectionString);
		}
	}
}