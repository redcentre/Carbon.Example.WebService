using System.Collections.Generic;

namespace Carbon.Examples.WebService.Common
{
	/// <summary>
	/// Contains environmental and version information about the web service.
	/// </summary>
	public class ServiceInfo
	{
		public string? Version { get; set; }
		public string? FileVersion { get; set; }
		public string? Build { get; set; }
		public string? Copyright { get; set; }
		public string? Company { get; set; }
		public string? Product { get; set; }
		public string? Title { get; set; }
		public string? Description { get; set; }
		public string? HostMachine { get; set; }
		public string? HostAccount { get; set; }
		public int? ProcessorCount { get; set; }
		public string? CarbonVersion { get; set; }
		public string? CarbonFileVersion { get; set; }
		public string? CarbonBuild { get; set; }
		public string? TempFolder { get; set; }
		public string? LicensingProvider { get; set; }
		public string? LicensingUri { get; set; }
		public override string ToString() => $"{GetType().Name}({Version},{Build},{CarbonVersion},{CarbonBuild},{HostMachine},{LicensingProvider})";
	}
}
