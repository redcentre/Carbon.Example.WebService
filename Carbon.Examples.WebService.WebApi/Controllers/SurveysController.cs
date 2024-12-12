using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RCS.Carbon.Export;
using TSAPI.Public.Domain.Metadata;

namespace Carbon.Examples.WebService.WebApi.Controllers;

//partial class SurveysController
//{
//	async Task<ActionResult<SurveyDetail[]>> TsapiStandardListImpl()
//	{
//		using var wrap = new StateWrap(SessionId, LicProv, false);
//		var export = new ExportEngine();
//		export.AttachJob(wrap.Engine);
//		return await export.ListTSAPIVisibleJobs();
//	}
//}
