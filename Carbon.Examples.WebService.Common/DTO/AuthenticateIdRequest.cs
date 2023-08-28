namespace Carbon.Examples.WebService.Common
{
	public sealed class AuthenticateIdRequest
	{
		public AuthenticateIdRequest(string id, string password, bool skipCache = false)
		{
			Id = id;
			Password = password;
			SkipCache = skipCache;
		}

		public string Id { get; set; }

		public string Password { get; set; }

		public bool SkipCache { get; set; } = false;
	}
}
