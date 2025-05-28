namespace RCS.Carbon.Example.WebService.Common
{
	public sealed class ReadTimingRequest2
	{
		public ReadTimingRequest2(string customer, string job, string vars, int count)
		{
			Customer = customer;
			Job = job;
			Vars = vars;
			Count = count;
		}

		public string Customer { get; set; }

		public string Job { get; set; }

		public string Vars { get; set; }

		public int Count { get; set; }
	}
}
