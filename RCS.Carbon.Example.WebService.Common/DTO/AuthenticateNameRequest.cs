namespace RCS.Carbon.Example.WebService.Common
{
	public sealed class AuthenticateNameRequest
	{
		public AuthenticateNameRequest(string name, string password, bool skipCache = false)
		{
			Name = name;
			Password = password;
			SkipCache = skipCache;
		}

		public string Name { get; set; }

		public string Password { get; set; }

		public bool SkipCache { get; set; } = false;
	}
}
