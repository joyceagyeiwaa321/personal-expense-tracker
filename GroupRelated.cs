using System;
using System.Collections.Generic;

namespace FinancyApplication
{
    public class Group
    {
        public int GroupID { get; set; }
        public int CreatedByUserID { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }

        public Group()
        {
        }

        public Group(int groupId, int createdByUserId, string name)
        {
            GroupID = groupId;
            CreatedByUserID = createdByUserId;
            Name = name;
            CreatedAt = DateTime.Now;
        }

        public int Create()
        {
            Data db = new Data();
            return db.InsertGroup(this);
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

        public override string ToString()
        {
            return $"GroupID: {GroupID}, Name: {Name}, CreatedByUserID: {CreatedByUserID}";
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
            return $"GroupID: {GroupID}, UserID: {UserID}, JoinedAt: {JoinedAt}";
        }
    }
}