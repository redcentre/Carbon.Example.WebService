namespace Carbon.Examples.WebService.Common
{
	public sealed class SessionCust
	{
		public string? Id { get; set; }
		public string? Name { get; set; }
		public string? DisplayName { get; set; }
		public string? AgencyId { get; set; }
		public SessionJob[]? SessionJobs { get; set; }
		public string? Info { get; set; }
		public string? Logo { get; set; }
		public string? Url { get; set; }
		public int? Sequence { get; set; }
		public string? StorageKey { get; set; }
		public SessionAgency? ParentAgency { get; set; }
	}
}
