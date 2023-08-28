namespace Carbon.Examples.WebService.Common
{
	public sealed class ReadTimingRequest1
	{
		public ReadTimingRequest1(string azconnect, string container, string names, int count, bool useCache)
		{
			AzConnect = azconnect;
			Container = container;
			Names = names;
			Count = count;
			UseCache = useCache;
		}

		public string AzConnect { get; set; }

		public string Container { get; set; }

		public string Names { get; set; }

		public int Count { get; set; }

		public bool UseCache { get; set; }
	}
}
