using System;
using MySql.Data.MySqlClient;

namespace ExpenseTracker
{
    public class DatabaseConnection
    {
        private static string connectionString =
            "Server=localhost;Port=3306;Database=expense_tracker;Uid=root;Pwd=;";

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }
    }
}