using System;
using System.Collections.Generic;
using System.Windows;

namespace FinancyApplication
{
	public class Transaction
	{
		private Data data = new Data();

		public int TransactionID { get; set; }
		public int UserID { get; set; }
		public int AccountID { get; set; }
		public int CategoryID { get; set; }
		public string CategoryName { get; set; }
		public int GroupID { get; set; }
		public int RecurringID { get; set; }
		public string Type { get; set; }
		public decimal Amount { get; set; }
		public string Description { get; set; }
		public string Status { get; set; }
		public DateTime Date { get; set; }

		public User User
		{
			get => default;
			set
			{
			}
		}

		public Account Account
		{
			get => default;
			set
			{
			}
		}

		public Category Category
		{
			get => default;
			set
			{
			}
		}

		public Group Group
		{
			get => default;
			set
			{
			}
		}

		public RecurringTransaction RecurringTransaction
		{
			get => default;
			set
			{
			}
		}

		public Transaction() { }

		public Transaction(int userId, int accountId, int categoryId, string type, decimal amount, string description)
		{
			this.UserID = userId;
			this.AccountID = accountId;
			this.CategoryID = categoryId;
			this.Type = type;
			this.Amount = amount;
			this.Description = description;
			this.Date = DateTime.Now;
			this.GroupID = 0;
		}

		public bool Create()
		{
			if (Amount <= 0)
			{
				MessageBox.Show("Transaction amount must be greater than zero.");
				return false;
			}

			try
			{
				this.TransactionID = data.InsertTransaction(this);
				return this.TransactionID > 0;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Create transaction error: " + ex.Message);
				return false;
			}
		}

		public void Update()
		{
			if (this.TransactionID <= 0)
			{
				MessageBox.Show("Cannot update a transaction that has not been saved yet.");
				return;
			}

			try
			{
				data.UpdateTransaction(this);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Update transaction error: " + ex.Message);
			}
		}

		public void Delete()
		{
			if (this.TransactionID <= 0)
			{
				MessageBox.Show("Cannot delete a transaction that has not been saved yet.");
				return;
			}

			try
			{
				data.DeleteTransaction(this.TransactionID);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Delete transaction error: " + ex.Message);
			}
		}

		public void AttachReceipt(Receipt receipt)
		{
			if (this.TransactionID <= 0)
			{
				MessageBox.Show("Transaction has not been saved yet.");
				return;
			}

			if (receipt == null || receipt.ReceiptID <= 0)
			{
				MessageBox.Show("Please provide a valid receipt.");
				return;
			}

			try
			{
				data.AttachReceiptToTransaction(this.TransactionID, receipt.ReceiptID);
			}
			catch (Exception ex)
			{
				MessageBox.Show("AttachReceipt error: " + ex.Message);
			}
		}

		public void Categorize(int categoryId)
		{
			if (this.TransactionID <= 0)
			{
				MessageBox.Show("Transaction has not been saved yet.");
				return;
			}

			try
			{
				this.CategoryID = categoryId;
				data.UpdateTransactionCategory(this.TransactionID, categoryId);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Categorize error: " + ex.Message);
			}
		}

		public override string ToString()
		{
			return "[" + Type + "] " + Amount + " - " + Description;
		}
	}

	public class Category
	{
		private Data data = new Data();
		public int CategoryID { get; set; }
		public int UserID { get; set; }
		public string Name { get; set; }
		public string Type { get; set; }
		public bool IsDefault { get; set; }

		public User User
		{
			get => default;
			set
			{
			}
		}

		public Category() { }

		public Category(int userId, string name, string type)
		{
			this.UserID = userId;
			this.Name = name;
			this.Type = type;
		}

		public bool Create()
		{
			if (string.IsNullOrWhiteSpace(Name))
			{
				MessageBox.Show("Please provide a category name.");
				return false;
			}

			try
			{
				this.CategoryID = data.InsertCategory(this);
				return this.CategoryID > 0;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Create category error: " + ex.Message);
				return false;
			}
		}

		public void Update(string newName)
		{
			if (this.CategoryID <= 0)
			{
				MessageBox.Show("Category has not been saved yet.");
				return;
			}

			if (string.IsNullOrWhiteSpace(newName))
			{
				MessageBox.Show("Please provide a valid category name.");
				return;
			}

			try
			{
				this.Name = newName;
				data.UpdateCategory(this.CategoryID, newName);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Update category error: " + ex.Message);
			}
		}

		public void Delete()
		{
			if (this.CategoryID <= 0)
			{
				MessageBox.Show("Cannot delete a category that has not been saved yet.");
				return;
			}

			if (IsDefault)
			{
				MessageBox.Show("Cannot delete a default category.");
				return;
			}

			try
			{
				data.DeleteCategory(this.CategoryID);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Delete category error: " + ex.Message);
			}
		}

		public List<Transaction> GetTransactions()
		{
			try
			{
				return data.GetTransactionsByCategory(this.CategoryID);
			}
			catch (Exception ex)
			{
				MessageBox.Show("GetTransactions error: " + ex.Message);
				return new List<Transaction>();
			}
		}

		public static void CreateDefaultCategories(int userId)
		{

			string[] defaults = { "Food & Dining", "Transport", "Housing", "Health", "Entertainment", "Salary" };

			foreach (string name in defaults)
			{
				string type = "expense";
				if (name == "Salary")
				{
					type = "income";
				}

				Category cat = new Category(userId, name, type);
				cat.IsDefault = true;
				cat.Create();
			}
		}

	}
}