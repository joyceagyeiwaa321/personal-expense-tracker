using System;

namespace ExpenseTracker
{
    public class Budget
    {
        public int BudgetId { get; set; }
        public int UserId { get; set; }
        public int CategoryId { get; set; }
        public decimal LimitAmount { get; set; }
        public string Month { get; set; }

        public Budget()
        {
        }

        public Budget(int budgetId, int userId, int categoryId, decimal limitAmount, string month)
        {
            BudgetId = budgetId;
            UserId = userId;
            CategoryId = categoryId;
            LimitAmount = limitAmount;
            Month = month;
        }

        public void Create()
        {
            Console.WriteLine("Budget created successfully.");
        }

        public void Update(decimal newLimitAmount)
        {
            LimitAmount = newLimitAmount;
            Console.WriteLine("Budget updated successfully.");
        }

        public void Delete()
        {
            Console.WriteLine("Budget deleted successfully.");
        }

        public decimal GetSpentAmount()
        {
            return 0;
        }

        public decimal GetRemainingAmount()
        {
            return LimitAmount - GetSpentAmount();
        }

        public bool IsExceeded()
        {
            return GetSpentAmount() > LimitAmount;
        }

        public override string ToString()
        {
            return $"BudgetId: {BudgetId}, LimitAmount: €{LimitAmount}, Month: {Month}";
        }
    }

    public class RecurringTransaction

        {
        public int RecurringId { get; set; }
        public int AccountId { get; set; }
        public int CategoryId { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public string Frequency { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime NextRunDate { get; set; }
        public bool IsActive { get; set; }
        public RecurringTransaction()
        {
        }
        public RecurringTransaction(int recurringId, int accountId, int categoryId, string type, decimal amount, string frequency, DateTime startDate)
        {
            RecurringId = recurringId;
            AccountId = accountId;
            CategoryId = categoryId;
            Type = type;
            Amount = amount;
            Frequency = frequency;
            StartDate = startDate;
            NextRunDate = startDate;
            IsActive = true;

        }

        public void Create()
        {
            Console.WriteLine("Recurring transaction created successfully.");
        }
        public void Pause()
        {
            IsActive = false;
            Console.WriteLine("Recurring transaction paused.");
        }
        public void Resume()
        {
            IsActive = true;
            Console.WriteLine("Recurring transaction resumed.");
        }
        public void Cancel()
        {
            IsActive = false;
            Console.WriteLine("Recurring transaction cancelled.");
        }

        public void Execute()
        {
            if (IsActive)
            {
                Console.WriteLine($"Recurring transaction executed: {Type} €{Amount}");
                UpdateNextRunDate();
            }
            else
            {
                Console.WriteLine("Recurring transaction is not active.");
            }
        }

        public void UpdateNextRunDate()
        {
            switch (Frequency.ToLower())
            {
                case "daily":
                    NextRunDate = NextRunDate.AddDays(1);
                    break;
                case "weekly":
                    NextRunDate = NextRunDate.AddDays(7);
                    break;
                case "monthly":
                    NextRunDate = NextRunDate.AddMonths(1);
                    break;
                case "yearly":
                    NextRunDate = NextRunDate.AddYears(1);
                    break;
                default:
                    Console.WriteLine("Invalid frequency.");
                    break;
            }
        }
        
        public override string ToString()
        {
            return $"RecurringId: {RecurringId}, Type: {Type}, Amount: €{Amount}, Frequency: {Frequency}";
        }
    }

    public class Receipt
    {
        public int ReceiptId { get; set; }
        public int TransactionId { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public DateTime UploadedAt { get; set; }

        public Receipt()
        { 
        }
         
        public Receipt(int receiptId, int transactionId, string filePath, string fileType)
        {
            ReceiptId = receiptId;
            TransactionId = transactionId;
            FilePath = filePath;
            FileType = fileType;
            UploadedAt = DateTime.Now;
        }

        public void Upload()
        {
            Console.WriteLine("Receipt uploaded successfully.");
        }

        public void Delete()
        {
            Console.WriteLine("Receipt deleted successfully.");
        }

        public string GetDownloadUrl()
        {
            return FilePath;
        }

        public override string ToString()
        {
            return $"ReceiptId: {ReceiptId}, TransactionId: {TransactionId}, File: {FilePath}";
        }
    }


}