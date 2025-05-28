namespace RCS.Carbon.Example.WebService.Common
{
	public sealed class GenTabPythonRequest
	{
#pragma warning disable CS8618     // Empty ctor required for JSON serialization                                                   
		public GenTabPythonRequest()
		{
		}
#pragma warning restore CS8618

		public string? Id { get; set; }
		public string? password { get; set; }
		public string? Name { get; set; }
		public string? CustomerName { get; set; }
		public string? JobName { get; set; }
		public string Top { get; set; }
		public string Side { get; set; }
		public string? Filter { get; set; }
		public string? Weight { get; set; }
		public bool? ColumnPercents { get; set; }
		public int FormatType { get; set; } = 1;
		public bool SkipCache { get; set; }
	}
}
