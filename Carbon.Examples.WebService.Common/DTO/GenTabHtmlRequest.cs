namespace Carbon.Examples.WebService.Common
{
	public sealed class GenTabHtmlRequest
	{
		public GenTabHtmlRequest(string top, string side, string? filter = null, string? weight = null, string? caseFilter = null)
		{
			Top = top;
			Side = side;
			Filter = filter;
			Weight = weight;
			CaseFilter = caseFilter;
		}

		public string Top { get; set; }
		public string Side { get; set; }
		public string? Filter { get; set; }
		public string? Weight { get; set; }
		public string? CaseFilter { get; set; }
	}
}
