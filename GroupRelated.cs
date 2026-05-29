using System;
using System.Collections.Generic;
using System.Linq;

namespace FinancyApplication
{
	public class Group
	{
		public int GroupID { get; set; }
		public int CreatedByUserID { get; set; }
		public string Name { get; set; }
		public string Description { get; set; } // New
		public string InviteCode { get; set; }  // New
		public DateTime CreatedAt { get; set; }
        public int MemberCount { get; set; }

        public Group()
		{
		}

		// Updated constructor for creating new groups with a code
		public Group(int createdByUserId, string name, string description)
		{
			CreatedByUserID = createdByUserId;
			Name = name;
			Description = description;
			InviteCode = GenerateRandomCode();
			CreatedAt = DateTime.Now;
		}

		private string GenerateRandomCode()
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			Random random = new Random();
			string result = "";
			for (int i = 0; i < 6; i++)
			{
				result += chars[random.Next(chars.Length)];
			}
			return result;
		}

		public int Create()
		{
			Data db = new Data();
			return db.InsertGroup(this);
		}

		// This is what the "Join" button uses
		public static int JoinByCode(int userId, string code)
		{
			Data db = new Data();
			int groupId = db.GetGroupIdByCode(code);

			if (groupId > 0)
			{
				GroupMember member = new GroupMember(groupId, userId);
				member.Join();
				return groupId;
			}
			return 0;
		}

		public void Update(string newName)
		{
			Name = newName;
			Data db = new Data();
			db.UpdateGroup(this);
		}

		public void Delete()
		{
			Data db = new Data();
			db.DeleteGroup(GroupID);
		}

		public GroupMember AddMember(int userId)
		{
			GroupMember member = new GroupMember(GroupID, userId);
			member.Join();
			return member;
		}

		public void RemoveMember(int userId)
		{
			Data db = new Data();
			db.DeleteGroupMember(GroupID, userId);
		}

		public List<GroupMember> GetMembers()
		{
			Data db = new Data();
			return db.GetGroupMembers(GroupID);
		}

		public List<Transaction> GetTransactions()
		{
			Data db = new Data();
			return db.GetTransactionsByGroup(GroupID);
		}

		public override string ToString()
		{
			return "GroupID: " + GroupID + ", Name: " + Name + ", Code: " + InviteCode;
		}
	}

	public class GroupMember
	{
		public int GroupID { get; set; }
		public int UserID { get; set; }
		public DateTime JoinedAt { get; set; }

		public GroupMember()
		{
		}

		public GroupMember(int groupId, int userId)
		{
			GroupID = groupId;
			UserID = userId;
			JoinedAt = DateTime.Now;
		}

		public void Join()
		{
			Data db = new Data();
			db.InsertGroupMember(this);
		}

		public void Leave()
		{
			Data db = new Data();
			db.DeleteGroupMember(GroupID, UserID);
		}

		public Group GetGroup()
		{
			Data db = new Data();
			return db.GetGroupById(GroupID);
		}

		public User GetUser()
		{
			Data db = new Data();
			return db.GetUserById(UserID);
		}

		public override string ToString()
		{
			return "GroupID: " + GroupID + ", UserID: " + UserID + ", JoinedAt: " + JoinedAt;
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