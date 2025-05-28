namespace RCS.Carbon.Example.WebService.Common
{
	public sealed class DashboardRequest
	{
		public DashboardRequest()
		{
		}

		public DashboardRequest(string customerName, string jobName)
			: this(customerName, jobName, null)
		{
		}

		public DashboardRequest(string customerName, string jobName, string? dashboardName)
		{
			CustomerName = customerName;
			JobName = jobName;
			DashboardName = dashboardName;
		}

		public string CustomerName { get; set; }
		public string JobName { get; set; }
		public string? DashboardName { get; set; }
	}
}
