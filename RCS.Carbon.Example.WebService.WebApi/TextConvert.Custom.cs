using System.Linq;
using RCS.Carbon.Example.WebService.Common.DTO;
using Lines = System.Collections.Generic.List<string>;

namespace RCS.Carbon.Example.WebService.WebApi;

partial class TextConvert
{
	/// <summary>
	/// Custom text serialization of a licence authentication response which contains nested class properties.
	/// </summary>
	static void SerializeToLines(SessionInfo value, Lines lines)
	{
		lines.Add($"{nameof(SessionInfo.SessionId)}={value.SessionId}");
		lines.Add($"{nameof(SessionInfo.Id)}={value.Id}");
		lines.Add($"{nameof(SessionInfo.Name)}={value.Name}");
		lines.Add($"{nameof(SessionInfo.Email)}={value.Email}");
		lines.Add($"{nameof(SessionInfo.ProcessorCount)}={value.ProcessorCount}");
		lines.Add($"{nameof(SessionInfo.OS)}={value.OS}");
		string? join = value.Roles == null ? null : string.Join(',', value.Roles);
		lines.Add($"{nameof(SessionInfo.Roles)}={join}");
		join = value.VartreeNames == null ? null : string.Join(',', value.VartreeNames);
		lines.Add($"{nameof(SessionInfo.VartreeNames)}={join}");
		if (value.SessionCusts?.Length > 0)
		{
			lines.Add("# Customers, Jobs and Vartrees are normally a 3 level tree.");
			lines.Add("# For the text response they are flattened into their logical node walking sequence.");
			lines.Add("# ║ Customer[cix]=Id,Name,DisplayName,AzureConnect");
			lines.Add("# ║ Job[jix]=Id,Name,DisplayName,Description");
			lines.Add("# ║ VartreeNames=V1,V2,V3,etc");
			lines.Add("# ║ RealCloudVartreeNames=V1,V2,V3,etc");
			foreach (var ctup in value.SessionCusts.Select((c, ci) => new { c, ci }))
			{
				lines.Add($"Customer[{ctup.ci}]={ctup.c.Id},{ctup.c.Name},{ctup.c.DisplayName},{ctup.c.StorageKey}");
				if (ctup.c.SessionJobs?.Length > 0)
				{

					foreach (var jtup in ctup.c.SessionJobs.Select((j, ji) => new { j, ji }))
					{
						lines.Add($"Job[{jtup.ji}]={jtup.j.Id},{jtup.j.Name},{jtup.j.DisplayName},{jtup.j.Description}");
						join = jtup.j.VartreeNames == null ? null : string.Join(',', jtup.j.VartreeNames);
						lines.Add($"VartreeNames={join}");
						join = jtup.j.RealCloudVartreeNames == null ? null : string.Join(',', jtup.j.RealCloudVartreeNames);
						lines.Add($"RealCloudVartreeNames={join}");
					}
				}
			}
		}
	}
}
