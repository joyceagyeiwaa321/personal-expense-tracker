using System;
using System.Collections.Generic;
using System.Windows;

namespace FinancyApplication
{
	public class Account
	{
		private Data data = new Data();

		public int AccountID { get; set; }
		public int UserID { get; set; }
		public string Name { get; set; }
		public string AccountType { get; set; }
		public decimal Balance { get; set; }
		public string Currency { get; set; }
		public DateTime CreatedAt { get; set; }

		public Account() { }

		public Account(int userId, string name, string type, decimal initialBalance, string currency)
		{
			this.UserID = userId;
			this.Name = name;
			this.AccountType = type;
			this.Balance = initialBalance;
			this.Currency = currency;
			this.CreatedAt = DateTime.Now;
		}

		public bool Save()
		{
			if (string.IsNullOrWhiteSpace(Name))
			{
				MessageBox.Show("Please provide an account name.");
				return false;
			}

			this.AccountID = data.InsertAccount(this);
			return this.AccountID > 0;
		}

		// Fixed: passes the delta (amount) to Data — SQL now does Balance = Balance + amount
		public void UpdateBalance(decimal amount)
		{
			this.Balance += amount;
			data.UpdateAccountBalance(this.AccountID, amount);
		}

		// Returns the full transaction history for this account
		public List<Transaction> GetTransactionHistory()
		{
			return data.GetTransactionsByAccount(this.AccountID);
		}

		// Renames the account both in memory and in the DB
		public void Rename(string newName)
		{
			if (string.IsNullOrWhiteSpace(newName))
			{
				MessageBox.Show("Please provide a valid account name.");
				return;
			}

			this.Name = newName;
			data.RenameAccount(this.AccountID, newName);
		}

		// Deletes the account from the DB
		public void Delete()
		{
			data.DeleteAccount(this.AccountID);
		}

		public override string ToString() => $"{Name}: {Balance} {Currency}";
	}
}