namespace RCS.Carbon.Example.WebService.Common.DTO;

public sealed class UpdateAccountRequest
{
	public UpdateAccountRequest()
	{
	}

	public UpdateAccountRequest(string userId, string userName, string comment, string email)
	{
		UserId = userId;
		UserName = userName;
		Comment = comment;
		Email = email;
	}

	public string UserId { get; set; }
	public string UserName { get; set; }
	public string Comment { get; set; }
	public string Email { get; set; }
}
