using System.Threading.Tasks;
using Carbon.Examples.WebService.Common;
using RCS.Azure.Data.Common;

namespace Carbon.Examples.WebService.WebApi.Controllers;

// COMPLEXITY WARNING -- A user session has access to one or more customers (storage accounts)
// as defined in their licensing record and their details are in the response from a login
// request. The customer-storagekey pairs are held in the session so that a request to process
// work in a specific customer can proceed by finding the key.

partial class DashboardController
{
	async Task<AzDashboard[]> ListDashboardsImpl(string customerName, string jobName)
	{
		return await AzProc.ListDashboardsAsync(GetKey(customerName), jobName, VDirName);
	}

	async Task<AzDashboard> GetDashboardImpl(DashboardRequest request)
	{
		return await AzProc.GetDashboardAsync(GetKey(request.CustomerName), request.JobName, request.DashboardName, VDirName);
	}

	async Task<bool> DeleteDashboardImpl(DashboardRequest request)
	{
		return await AzProc.DeleteDashboardAsync(GetKey(request.CustomerName), request.JobName, request.DashboardName, VDirName);
	}

	async Task<AzDashboard> UpsertDashboardImpl(UpsertDashboardRequest request)
	{
		return await AzProc.UpsertDashboardAsync(GetKey(request.CustomerName), request, VDirName);
	}

	string GetKey(string customerName)
	{
		SessionItem item = SessionManager.FindSession(SessionId, true)!;
		return item.FindStorageKey(customerName, true)!;
	}

	string VDirName => Config["CarbonApi:DashboardsVDirName"]!;
}
