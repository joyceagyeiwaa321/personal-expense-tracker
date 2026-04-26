using System;

namespace FinancyApplication
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

        public int Create()
        {
            Data db = new Data();
            Console.WriteLine("Budget created successfully.");
            return db.InsertBudget(this);
        }

        public void Update(decimal newLimitAmount)
        {
            LimitAmount = newLimitAmount;
            
            Data db = new Data();
            db.UpdateBudget(this);
             Console.WriteLine("Budget updated successfully.");
        }

        public void Delete()
        {
            Data db = new Data();
            db.DeleteBudget(BudgetId);
            Console.WriteLine("Budget deleted successfully.");
        }

        public decimal GetSpentAmount()
        {
            Data db = new Data();
            return db.GetSpentAmount(UserId, CategoryId, Month);
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

        public int Create()
        {
            Data db = new Data();
            Console.WriteLine("Recurring transaction created successfully.");
            return db.InsertRecurringTransaction(this);
        }
        public void Pause()
        {
            IsActive = false;
            Data db = new Data();
            db.UpdateRecurringTransaction(this);
            Console.WriteLine("Recurring transaction paused.");
        }
        public void Resume()
        {
            IsActive = true;
            Data db = new Data();
            db.UpdateRecurringTransaction(this);
            Console.WriteLine("Recurring transaction resumed.");
        }
        public void Cancel()
        {
            IsActive = false;

            Data db = new Data();
            db.DeleteRecurringTransaction(RecurringId);
            Console.WriteLine("Recurring transaction cancelled.");
        }

        public void Execute()
        {
            if (IsActive)
            {
                Console.WriteLine($"Recurring transaction executed: {Type} €{Amount}");
                UpdateNextRunDate();
                Data db = new Data();
                db.UpdateRecurringTransaction(this);
            }
            else
            {
                Data db = new Data();
                db.UpdateRecurringTransaction(this);
                Console.WriteLine("Recurring transaction is not active.");
            }
        }

        public void UpdateNextRunDate()
        {
            switch (Frequency?.ToLower())
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
        public int ReceiptID { get; set; }
        public int TransactionID { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public DateTime UploadedAt { get; set; }

        public Receipt()
        { 
        }
         
        public Receipt(int receiptId, int transactionId, string filePath, string fileType)
        {
            ReceiptID = receiptId;
            TransactionID = transactionId;
            FilePath = filePath;
            FileType = fileType;
            UploadedAt = DateTime.Now;
        }

        public int Upload()
        {
            Data db = new Data();
            Console.WriteLine("Receipt uploaded successfully.");
            return db.InsertReceipt(this);
        }

        public void Delete()
        {
            Data db = new Data();
            db.DeleteReceipt(ReceiptID);
            Console.WriteLine("Receipt deleted successfully.");
        }

        public string GetDownloadUrl()
        {
            return FilePath;
        }

        public override string ToString()
        {
            return $"ReceiptId: {ReceiptID}, TransactionId: {TransactionID}, File: {FilePath}";
        }
    }


}