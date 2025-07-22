namespace RCS.Carbon.Example.WebService.Common.DTO;

public sealed class AuthenticateIdRequest
{
	public AuthenticateIdRequest(string id, string password, bool skipCache = false, string? appId = null)
	{
		Id = id;
		Password = password;
		SkipCache = skipCache;
		Appid = appId;
	}

	public string Id { get; set; }

	public string Password { get; set; }

	public bool SkipCache { get; set; } = false;

	public string? Appid { get; set; }
}
