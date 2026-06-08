using System;

namespace FinancyApplication
{
	public class GroupInvite
	{
		public int InviteID { get; set; }
		public int GroupID { get; set; }
		public int FromUserID { get; set; }
		public int ToUserID { get; set; }
		public string Status { get; set; }
		public DateTime SentAt { get; set; }

		public Group Group
		{
			get => default;
			set
			{
			}
		}
	}
}