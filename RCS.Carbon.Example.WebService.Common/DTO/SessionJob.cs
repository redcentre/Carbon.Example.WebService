using System;
using System.Linq;

namespace RCS.Carbon.Example.WebService.Common
{
	public sealed class SessionJob
	{
		public string? Id { get; set; }
		public string? Name { get; set; }
		public string? DisplayName { get; set; }
		public string? Description { get; set; }
		/// <summary>
		/// The legacy list of vartree names in the job. The value is manually maintained and may
		/// not correspond to the real vartree blobs in the cloud job's container.
		/// </summary>
		public string[]? VartreeNames { get; set; }
		/// <summary>
		/// The list of 'real' vartree names of *.vtr blobs that are stored in the job's container.
		/// </summary>
		public string[]? RealCloudVartreeNames { get; set; }
		/// <summary>
		/// Attempts to get the 'best' list of job vartrees. If no 'real' list is available then the legacy
		/// list is returned, otherwise the old list is treated as an optional filter for the 'real' list.
		/// </summary>
		/// <returns></returns>
		public string[] GetBestVartees()
		{
			if (IsAccessible != true)
			{
				// Real names are unknown so fallback to the old names.
				return VartreeNames ?? Array.Empty<string>();
			}
			else
			{
				if (VartreeNames?.Length > 0)
				{
					// Return real names filtered down by old names.
					return RealCloudVartreeNames!.Where(r => VartreeNames!.Any(v => string.Compare(v, r, StringComparison.OrdinalIgnoreCase) == 0)).ToArray();
				}
				else
				{
					// Simply return the real name list.
					return RealCloudVartreeNames!;
				}
			}
		}
		/// <summary>
		/// True is the job's container was accessible and the <see cref="RealCloudVartreeNames"/> list is accurate,
		/// the list will be null for all other values. False if the job's container was inaccessible because it
		/// doesn't exist or there are security restrictions. Null if no attempt was made to access the job's container.
		/// </summary>
		public bool? IsAccessible { get; set; }
		public string? Info { get; set; }
		public string? Logo { get; set; }
		public string? Url { get; set; }
		public int? Sequence { get; set; }
	}
}
