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
		public int RecurringID { get; set; }
		public string Type { get; set; }
		public decimal Amount { get; set; }
		public string Description { get; set; }
		public DateTime Date { get; set; }

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
		}

		// Inserts this transaction into the DB and sets the TransactionID
		public bool Create()
		{
			if (Amount <= 0)
			{
				MessageBox.Show("Transaction amount must be greater than zero.");
				return false;
			}

			this.TransactionID = data.InsertTransaction(this);
			return this.TransactionID > 0;
		}

		// Updates the transaction record in the DB with current property values
		public void Update()
		{
			if (this.TransactionID <= 0)
			{
				MessageBox.Show("Cannot update a transaction that has not been saved yet.");
				return;
			}

			data.UpdateTransaction(this);
		}

		// Deletes this transaction from the DB
		public void Delete()
		{
			if (this.TransactionID <= 0)
			{
				MessageBox.Show("Cannot delete a transaction that has not been saved yet.");
				return;
			}

			data.DeleteTransaction(this.TransactionID);
		}

		// Links a Receipt to this transaction in the DB
		public void AttachReceipt(Receipt receipt)
		{
			if (receipt == null || receipt.ReceiptID <= 0)
			{
				MessageBox.Show("Please provide a valid receipt.");
				return;
			}

			data.AttachReceiptToTransaction(this.TransactionID, receipt.ReceiptID);
		}

		// Changes the category this transaction belongs to
		public void Categorize(int categoryId)
		{
			this.CategoryID = categoryId;
			data.UpdateTransactionCategory(this.TransactionID, categoryId);
		}

		public override string ToString() => $"[{Type}] {Amount} - {Description}";
	}

	public class Category
	{
		private Data data = new Data();

		public int CategoryID { get; set; }
		public int UserID { get; set; }
		public string Name { get; set; }
		public string Type { get; set; }
		public bool IsDefault { get; set; }

		public Category() { }

		public Category(int userId, string name, string type)
		{
			this.UserID = userId;
			this.Name = name;
			this.Type = type;
		}

		// Inserts this category into the DB and sets CategoryID
		public bool Create()
		{
			if (string.IsNullOrWhiteSpace(Name))
			{
				MessageBox.Show("Please provide a category name.");
				return false;
			}

			this.CategoryID = data.InsertCategory(this);
			return this.CategoryID > 0;
		}

		// Renames the category both in memory and in the DB
		public void Update(string newName)
		{
			if (string.IsNullOrWhiteSpace(newName))
			{
				MessageBox.Show("Please provide a valid category name.");
				return;
			}

			this.Name = newName;
			data.UpdateCategory(this.CategoryID, newName);
		}

		// Deletes this category from the DB
		public void Delete()
		{
			if (this.CategoryID <= 0)
			{
				MessageBox.Show("Cannot delete a category that has not been saved yet.");
				return;
			}

			data.DeleteCategory(this.CategoryID);
		}

		// Returns all transactions that belong to this category
		public List<Transaction> GetTransactions()
		{
			return data.GetTransactionsByCategory(this.CategoryID);
		}
	}
}