using System.Threading.Tasks;
using RCS.Carbon.Example.WebService.Common;
using RCS.Azure.Data.Common;

namespace RCS.Carbon.Example.WebService.WebApi.Controllers;

partial class DashboardController
{
	async Task<AzDashboard[]> ListDashboardsImpl(string customerName, string jobName)
	{
		LogInfo(400, "List dashboards {CustomerName} {JobName}", customerName, jobName);
		return await AzProc.ListDashboardsAsync(GetKey(customerName), jobName, VDirName);
	}

	async Task<AzDashboard> GetDashboardImpl(DashboardRequest request)
	{
		LogInfo(402, "Get dashboard {CustomerName} {JobName} {DashboardName}", request.CustomerName, request.JobName, request.DashboardName);
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

	string VDirName => Config["CarbonApi:DashboardsVDirName"]!;
}
