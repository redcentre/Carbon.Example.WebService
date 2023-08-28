namespace Carbon.Examples.WebService.Common
{
	public sealed class SessionInfo
	{
		public string? SessionId { get; set; }
		public string? Id { get; set; }
		public string? Name { get; set; }
		public string? Email { get; set; }
		public string[]? Roles { get; set; }
		public string[]? VartreeNames { get; set; }
		public SessionCust[]? SessionCusts { get; set; }
		public override string ToString() => $"{GetType().Name}({SessionId},{Id},{Name},R{Roles?.Length}, V{VartreeNames?.Length},C{SessionCusts?.Length})";
	}

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

	public sealed class SessionJob
	{
		public string? Id { get; set; }
		public string? Name { get; set; }
		public string? DisplayName { get; set; }
		public string? Description { get; set; }
		public string[]? VartreeNames { get; set; }
		public string? Info { get; set; }
		public string? Logo { get; set; }
		public string? Url { get; set; }
		public int? Sequence { get; set; }
	}

	public sealed class SessionAgency
	{
		public string? Id { get; set; }
		public string? Name { get; set; }
	}
}
