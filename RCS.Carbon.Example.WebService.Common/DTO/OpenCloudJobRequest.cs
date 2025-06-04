namespace RCS.Carbon.Example.WebService.Common.DTO;

public enum JobTocType
{
	None,
	ExecUser,
	Simple
}

public sealed class OpenCloudJobRequest
{
#pragma warning disable CS8618     // Empty ctor required for JSON serialization                                                   
	public OpenCloudJobRequest()
	{
	}
#pragma warning restore CS8618

	public OpenCloudJobRequest(string customerName, string jobName, string vartreeName)
		: this(customerName, jobName, vartreeName, false, false, false, JobTocType.ExecUser, false)
	{
	}

	public OpenCloudJobRequest(string customerName, string jobName, string? vartreeName, bool getDisplayProps, bool getVarteeNames, bool getAxisTreeNames, JobTocType tocType, bool getDrills)
	{
		CustomerName = customerName;
		JobName = jobName;
		VartreeName = vartreeName;
		GetDisplayProps = getDisplayProps;
		GetVartreeNames = getVarteeNames;
		GetAxisTreeNames = getAxisTreeNames;
		TocType = tocType;
		GetDrills = getDrills;
	}

	public string CustomerName { get; set; }
	public string JobName { get; set; }
	public string? VartreeName { get; set; }
	public bool GetDisplayProps { get; set; }
	public bool GetVartreeNames { get; set; }
	public bool GetAxisTreeNames { get; set; }
	public JobTocType TocType { get; set; }
	public bool GetDrills { get; set; }
}
