namespace Carbon.Examples.WebService.Common
{
	public sealed class ChangePasswordRequest
	{
		public ChangePasswordRequest()
		{
		}

		public ChangePasswordRequest(string userId, string oldPassword, string newPassword)
		{
			UserId = userId;
			OldPassword = oldPassword;
			Newpassword = newPassword;
		}

		public string UserId { get; set; }
		public string OldPassword { get; set; }
		public string Newpassword { get; set; }
	}
}
