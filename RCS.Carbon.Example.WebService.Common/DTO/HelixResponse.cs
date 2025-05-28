using System;

namespace RCS.Carbon.Example.WebService.Common
{
	public sealed class HelixResponse
	{
		public int Created { get; set; }
		public string Object { get; set; }
		public Guid Id { get; set; }
		public string Model { get; set; }
		public HelixChoice[] Choices { get; set; }
		public HelixUsage Usage { get; set; }
	}

	public sealed class HelixChoice
	{
		public int Index { get; set; }
		public string FinishReason { get; set; }
	}

	public sealed class HelixMessage
	{
		public string Role { get; set; }
		public string Content { get; set; }
	}

	public sealed class HelixUsage
	{
		public int PromptTokens { get; set; }
		public int CompletionTokens { get; set; }
		public int TotalTokens { get; set; }
	}
}
