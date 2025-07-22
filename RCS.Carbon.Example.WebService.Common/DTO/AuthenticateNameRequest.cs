namespace RCS.Carbon.Example.WebService.Common.DTO;

public sealed class AuthenticateNameRequest
{
	public AuthenticateNameRequest(string name, string password, bool skipCache = false, string? appId = null)
	{
		Name = name;
		Password = password;
		SkipCache = skipCache;
		Appid = appId;
	}

	public string Name { get; set; }

	public string Password { get; set; }

	public bool SkipCache { get; set; } = false;

	public string? Appid { get; set; }
}
