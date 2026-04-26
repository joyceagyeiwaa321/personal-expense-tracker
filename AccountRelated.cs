using System;
using System.Collections.Generic;
using System.Globalization;
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
		public string CurrencySymbol { get; set; }
		public DateTime CreatedAt { get; set; }

		public Account() { }

		public Account(int userId, string name, string type, decimal initialBalance, string dropdownValue)
		{
			this.UserID = userId;
			this.Name = name;
			this.AccountType = type;
			this.Balance = initialBalance;
			this.Currency = ExtractCurrencyCode(dropdownValue);
			this.CurrencySymbol = ExtractCurrencySymbol(dropdownValue);
			this.CreatedAt = DateTime.Now;
		}

		public static List<string> GetCurrencies()
		{
			List<string> currencies = new List<string>();
			List<string> alreadyAdded = new List<string>();

			foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
			{
				try
				{
					RegionInfo region = new RegionInfo(culture.Name);
					string code = region.ISOCurrencySymbol;
					string symbol = region.CurrencySymbol;
					string name = region.CurrencyEnglishName;
					string entry = $"{code} - {symbol} - {name}";

					if (!alreadyAdded.Contains(code))
					{
						alreadyAdded.Add(code);
						currencies.Add(entry);
					}
				}
				catch (Exception)
				{
					// Some cultures don't have valid region data, skip them
				}
			}

			currencies.Sort();
			return currencies;
		}

		public static string ExtractCurrencyCode(string dropdownValue)
		{
			return dropdownValue.Split("-")[0].Trim();
		}

		public static string ExtractCurrencySymbol(string dropdownValue)
		{
			return dropdownValue.Split("-")[1].Trim();
		}

		public bool Save()
		{
			if (string.IsNullOrWhiteSpace(Name))
			{
				MessageBox.Show("Please provide an account name.");
				return false;
			}

			if (this.UserID <= 0)
			{
				MessageBox.Show("Invalid user. Please log in again.");
				return false;
			}

			if (this.Balance < 0)
			{
				MessageBox.Show("Initial balance cannot be negative.");
				return false;
			}

			if (string.IsNullOrWhiteSpace(Currency))
			{
				MessageBox.Show("Please select a currency.");
				return false;
			}

			try
			{
				this.AccountID = data.InsertAccount(this);
				return this.AccountID > 0;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Save account error: " + ex.Message);
				return false;
			}
		}

		public void UpdateBalance(decimal amount)
		{
			if (this.AccountID <= 0)
			{
				MessageBox.Show("This account has not been saved yet.");
				return;
			}

			try
			{
				this.Balance += amount;
				data.UpdateAccountBalance(this.AccountID, amount);
			}
			catch (Exception ex)
			{
				MessageBox.Show("UpdateBalance error: " + ex.Message);
			}
		}

		public List<Transaction> GetTransactionHistory()
		{
			if (this.AccountID <= 0)
			{
				MessageBox.Show("This account has not been saved yet.");
				return new List<Transaction>();
			}

			try
			{
				return data.GetTransactionsByAccount(this.AccountID);
			}
			catch (Exception ex)
			{
				MessageBox.Show("GetTransactionHistory error: " + ex.Message);
				return new List<Transaction>();
			}
		}

		public void Rename(string name)
		{
			if (this.AccountID <= 0)
			{
				MessageBox.Show("This account has not been saved yet.");
				return;
			}

			if (string.IsNullOrWhiteSpace(name))
			{
				MessageBox.Show("Please provide a valid account name.");
				return;
			}

			try
			{
				this.Name = name;
				data.RenameAccount(this.AccountID, name);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Rename error: " + ex.Message);
			}
		}

		public void Delete()
		{
			if (this.AccountID <= 0)
			{
				MessageBox.Show("This account has not been saved yet.");
				return;
			}

			try
			{
				data.DeleteAccount(this.AccountID);
				this.AccountID = 0;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Delete account error: " + ex.Message);
			}
		}

		public override string ToString() => $"{Name}: {CurrencySymbol}{Balance} ({Currency})";
	}
}