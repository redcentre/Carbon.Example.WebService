namespace Carbon.Examples.WebService.Common
{
	public sealed class QuickUpdateRequest
	{
		public QuickUpdateRequest()
		{
		}

		public QuickUpdateRequest(bool showFreq, bool showColPct, bool showRowPct, bool showSig, string? filter)
		{
			ShowFreq = showFreq;
			ShowColPct = showColPct;
			ShowRowPct = showRowPct;
			ShowSig = showSig;
			Filter = filter;
		}

		public bool ShowFreq { get; set; }
		public bool ShowColPct { get; set; }
		public bool ShowRowPct { get; set; }
		public bool ShowSig { get; set; }
		public string? Filter { get; set; } // The full filter composed by the client

		public override string ToString()
		{
			return $"({ShowFreq},{ShowColPct},{ShowRowPct},{ShowSig},{Filter})";
		}
	}
}
