using System;
using System.Windows;

namespace FinancyApplication
{
    public class Goal
    {
        public int GoalId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal SavedAmount { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime CreatedAt { get; set; }

        public Goal()
        {
        }

        public Goal(int goalId, int userId, string name, decimal targetAmount,
            decimal savedAmount, DateTime? deadline)
        {
            GoalId = goalId;
            UserId = userId;
            Name = name;
            TargetAmount = targetAmount;
            SavedAmount = savedAmount;
            Deadline = deadline;
            CreatedAt = DateTime.Now;
        }

        public int Create()
        {
            Data db = new Data();
            int id = db.InsertGoal(this);
            return id;
        }

        public void Update()
        {
            Data db = new Data();
            db.UpdateGoal(this);
        }

        public void Delete()
        {
            Data db = new Data();
            db.DeleteGoal(GoalId);
        }

        public decimal GetRemainingAmount()
        {
            decimal remaining = TargetAmount - SavedAmount;
            return remaining < 0 ? 0 : remaining;
        }

        public double GetProgressPercent()
        {
            if (TargetAmount <= 0) return 0;
            double pct = (double)(SavedAmount / TargetAmount) * 100.0;
            if (pct < 0) return 0;
            if (pct > 100) return 100;
            return pct;
        }

        public bool IsCompleted()
        {
            return SavedAmount >= TargetAmount && TargetAmount > 0;
        }

        public string GetStatus()
        {
            return IsCompleted() ? "Completed" : "Ongoing";
        }

        public override string ToString()
        {
            return $"GoalId: {GoalId}, Name: {Name}, Target: €{TargetAmount}, Saved: €{SavedAmount}";
        }
    }

    public class ExpenseSplit
    {
        public int ExpenseSplitID { get; set; }
        public int TransactionID { get; set; }
        public int UserID { get; set; }
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }

        public ExpenseSplit()
        {
        }

        public ExpenseSplit(int transactionId, int userId, decimal amount, bool isPaid = false)
        {
            TransactionID = transactionId;
            UserID = userId;
            Amount = amount;
            IsPaid = isPaid;
            PaidAt = isPaid ? DateTime.Now : (DateTime?)null;
        }

        public override string ToString()
        {
            return $"ExpenseSplitID: {ExpenseSplitID}, TransactionID: {TransactionID}, UserID: {UserID}, Amount: {Amount}, IsPaid: {IsPaid}";
        }
    }
}