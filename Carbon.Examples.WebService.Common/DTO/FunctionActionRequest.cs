namespace Carbon.Examples.WebService.Common
{
	public enum FunctionAction
	{
		Create,
		Edit,
		Delete
	}

	public sealed class FunctionActionRequest
	{
		public FunctionActionRequest()
		{
		}

		public FunctionActionRequest(FunctionAction action, string expression, string? newExpression, string? label)
		{
			Action = action;
			Expression = expression;
			NewExpression = newExpression;
			Label = label;
		}

		public FunctionAction Action { get; set; }
		public string Expression { get; set; }
		public string? NewExpression { get; set; }
		public string? Label { get; set; }

		public override string ToString()
		{
			return $"({Action},{Expression},{NewExpression},{Label})";
		}
	}
}
