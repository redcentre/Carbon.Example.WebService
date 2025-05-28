namespace RCS.Carbon.Example.WebService.Common
{
	public sealed class ValidateExpRequest
	{
		public ValidateExpRequest()
		{
		}

		public ValidateExpRequest(string expression)
		{
			Expression = expression;
		}

		public string Expression { get; set; }
	}
}
