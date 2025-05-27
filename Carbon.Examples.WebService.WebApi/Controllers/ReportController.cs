using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Carbon.Examples.WebService.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RCS.Carbon.Shared;
using RCS.Carbon.Tables;
using RCS.RubyCloud.WebService;

namespace Carbon.Examples.WebService.WebApi.Controllers;

public enum TextOutputFormat
{
	TSV = 1,
	CSV = 2,
	SSV = 3,
	XML = 6,
	HTML = 7,
	OXT = 8,
	OXTNums = 9,
	MultiCube = 11
}

/// <ignore/>
public partial class ReportController : ServiceControllerBase
{
	#region Endpoints needing manual coding

	/// <summary>
	/// Generates a crosstab report as plain text in different formats.
	/// </summary>
	/// <param name="format">The text format for the generated report.</param>
	/// <param name="request">A serialized <c>GenTabRequest</c> provided in the request body.</param>
	/// <response code="200">The string body of a crosstab report as plain text.</response>
	/// <include file='DocInclude.xml' path='doc/members[@name="Auth403"]/*'/>
	/// <remarks>The response content-type is always text/plain.</remarks>
	[HttpPost]
	[Route("gentab/text/{format}")]
	[AuthFilter]
	[Produces(MediaTypeNames.Application.Json, MediaTypeNames.Text.Plain)]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK, MediaTypeNames.Text.Plain)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
	public async Task<ActionResult<string>> ReportGenTabText([FromRoute] TextOutputFormat format, [FromBody] GenTabRequest request)
	{
		request.DProps.Output.Format = (XOutputFormat)(int)format;
		using var wrap = new StateWrap(SessionId, LicProv, false);
		string report = wrap.Engine.GenTab(request.Name, request.Top, request.Side, request.Filter, request.Weight, request.SProps, request.DProps);
		LogInfo(510, "GenTab({Format},{Top},{Side},{Filter},{Weight})", request.DProps.Output.Format, request.Top, request.Side, request.Filter, request.Weight);
		var result = new ContentResult() { Content = report, ContentType = MediaTypeNames.Text.Plain, StatusCode = StatusCodes.Status200OK };
		return await Task.FromResult(result);
	}

	/// <summary>
	/// Generates a crosstab report as JSON compatible with a Python pandas DataFrame.
	/// </summary>
	/// <param name="shape">JSON response shape number 1, 2 or 3.</param>
	/// <param name="request">A serialized <c>GenTabRequest</c> provided in the request body.</param>
	/// <response code="200">The string body of a crosstab report as a JSON document.</response>
	/// <include file='DocInclude.xml' path='doc/members[@name="Auth403"]/*'/>
	/// <remarks>
	/// The <paramref name="shape"/> number values 1, 2 and 3 cause the response JSON to be in slightly
	/// different document shapes. All of the JSON document shapes contain the same data values and can
	/// be loaded directly into a pandas DataFrame. The caller can select the shape that is most suitable
	/// for their needs.
	/// </remarks>
	[HttpPost]
	[Route("gentab/pandas/{shape}")]
	[AuthFilter]
	[Produces(MediaTypeNames.Application.Json)]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> ReportGenTabPandas([FromRoute] int shape, [FromBody] GenTabRequest request)
	{
		using var wrap = new StateWrap(SessionId, LicProv, false);
		request.DProps.Cells.Frequencies.Visible = false;
		request.DProps.Cells.RowPercents.Visible = true;    // TODO ReportGenTabPandas Visibles ?
		request.DProps.Cells.ColumnPercents.Visible = false;
		request.DProps.Output.Format = XOutputFormat.None;
		wrap.Engine.GenTab(null, request.Top, request.Side, request.Filter, request.Weight, request.SProps, request.DProps);
		PandasRawData raw = wrap.Engine.GetTabAsPandas();
		object dict;
		if (shape == 1)
		{
			dict = raw.ToShape1();
		}
		else if (shape == 2)
		{
			dict = raw.ToShape2();
		}
		else
		{
			dict = raw.ToShape3();
		}
		return await Task.FromResult(Ok(dict));
	}

	#endregion

	async Task<ActionResult<XDisplayProperties>> GetPropsImpl()
	{
		using var wrap = new StateWrap(SessionId, LicProv, false);
		XDisplayProperties jobprops = wrap.Engine.GetProps();
		string json = JsonSerializer.Serialize(jobprops);
		return await Task.FromResult(jobprops);
	}

	async Task<ActionResult<XlsxResponse>> SetPropsImpl(XDisplayProperties request)
	{
		string json = JsonSerializer.Serialize(request);
		var watch = new Stopwatch();
		watch.Start();
		using var wrap = new StateWrap(SessionId, LicProv, true);
		wrap.Engine.SetProps(request);
		LogInfo(264, "Set props");
		return await MakeXlsxAndUpload(wrap, "SetProps");
	}

	async Task<ActionResult<string[]>> GenTabImpl(GenTabRequest request)
	{
		using var wrap = new StateWrap(SessionId, LicProv, true);
		string[] lines;
		if (request.DProps.Output.Format == XOutputFormat.XLSX)
		{
			// XLSX Excel output is a special case and does not return a typical set of report lines.
			// It natively generates the buffer of an XLSX workbook, which is converted to a single
			// base64 encoded string line for return.
			byte[] buff = wrap.Engine.GenTabAsXLSX(request.Name, request.Top, request.Side, request.Filter, request.Weight, request.SProps, request.DProps);
			lines = new string[] { Convert.ToBase64String(buff) };
		}
		else
		{
			// All other reports can be returned as lines. The lines maybe null for format None.
			string report = wrap.Engine.GenTab(request.Name, request.Top, request.Side, request.Filter, request.Weight, request.SProps, request.DProps);
			if (report == null)
			{
				return NoContent();
			}
			lines = [.. CommonUtil.ReadStringLines(report)];
		}
		LogInfo(230, "GenTab({Format},{Top},{Side},{Filter},{Weight}) -> #{Length})", request.DProps.Output.Format, request.Top, request.Side, request.Filter, request.Weight, lines?.Length);
		return await Task.FromResult(lines);
	}

	async Task<ActionResult<GenericResponse>> LoadReportImpl(LoadReportRequest request)
	{
		using var wrap = new StateWrap(SessionId, LicProv, true);
		SessionManager.SetReportName(SessionId, request.Name);
		wrap.Engine.TableLoadCBT(request.Name);
		LogInfo(232, "LoadReport {Name}", request.Name);
		return await Task.FromResult(new GenericResponse(0, $"Loaded {request.Name}"));
	}

	async Task<ActionResult<bool>> UnloadReportImpl()
	{
		using var wrap = new StateWrap(SessionId, LicProv, true);
		bool changed = SessionManager.SetReportName(SessionId, null);
		return await Task.FromResult(changed);
	}

	async Task<ActionResult<XlsxResponse>> GenerateXlsxImpl()
	{
		var watch = new Stopwatch();
		watch.Start();
		using var wrap = new StateWrap(SessionId, LicProv, true);
		return await MakeXlsxAndUpload(wrap, "Generate");
	}

	/// <summary>
	/// This method generates multiple OXTs sequentially in a single call. It's only used in unit tests at
	/// the moment because it will probably cause web service call from a client to timeout.
	/// </summary>
	async Task<ActionResult<MultiOxtResponse>> MultiOxtImpl(MultiOxtRequest request)
	{
		var moxt = new MoxtState(request);
		MultiOxtSequentialProc(moxt);
		var response = new MultiOxtResponse
		{
			Id = moxt.Id,
			Created = moxt.Created,
			ProgressMessage = moxt.ProgressMessage,
			Items = moxt.Items
		};
		return await Task.FromResult(response);
	}

	DateTime multiOxtStartTime;

	/// <summary>
	/// This starts the asynchronous generation of mutiple OXTs. The processing runs on a dedicated
	/// Thread and the client can call MultiOxtQuery to track progress, using the Guid Id returned
	/// here as the key to the processing. Rememeber that this web session only lasts for the duration
	/// of this method it cannot be referenced from the async thread. The caller can request single-thread
	/// sequential processing in the traditional way, or parallel processing on multiple cores. The actual
	/// number cores used will be limited to the number available.
	/// </summary>
	async Task<ActionResult<Guid>> MultiOxtStartImpl(MultiOxtRequest request)
	{
		multiOxtStartTime = DateTime.Now;
		LogInfo(240, "MultiOxtStartImpl Enter");
		var state = MakeState(request);
		state.SessionId = SessionId;
		state.ParallelCount = request.ParallelCount;
		ParameterizedThreadStart proc = request.ParallelCount > 1 ? MultiOxtParallelProc : MultiOxtSequentialProc;
		var t = new Thread(proc);
		t.Start(state);
		LogInfo(242, "MultiOxtStartImpl Exit {StateId} tid={ManagedThreadId}", state.Id, t.ManagedThreadId);
		return await Task.FromResult(state.Id);
	}

	/// <summary>
	/// Queries the progress of multi OXT processing running on a dedicated thread, using the Guid Id
	/// that was returned by MultiOxtStart.
	/// </summary>
	async Task<ActionResult<MultiOxtResponse>> MultiOxtQueryImpl(Guid id)
	{
		var response = new MultiOxtResponse();
		var moxt = GetState(id);
		if (moxt != null)
		{
			response.Id = moxt.Id;
			response.Created = moxt.Created;
			response.ProgressMessage = moxt.ProgressMessage;
			response.IsCancelled = moxt.CancelSource.IsCancellationRequested;
			response.ParallelCount = moxt.ParallelCount;
			response.Items = moxt.Items;
			if (moxt.Items != null)
			{
				// When the Items array has a value then the loop over the multi-reports
				// is finished and we can remove the state. The caller must recognise that
				// the Items are present and realise that the reports are finished.
				RemoveState(moxt.Id);
				LogDebug(250, "Multi OXT Id {Id} complete and removed (count down to {MoxtCount})", id, MoxtList.Count);
			}
			else
			{
				//Global.LogInfo(893, $"Multi OXT Id {id} running - {moxt.ProgressMessage}");
			}
		}
		else
		{
			// There is no specific error return for this. The returned Id will be Guid.Empty.
			//Global.LogInfo(894, $"Multi OXT Id {id} not found in {Global.StateCount} items");
		}
		return await Task.FromResult(response);
	}

	async Task<ActionResult<bool>> MultiOxtCancelImpl(Guid id)
	{
		bool success = CancelState(id);
		return await Task.FromResult(success);
	}

	async Task<ActionResult<GenericResponse>> DeleteInUserTocImpl(DeleteInUserTocRequest request)
	{
		using var wrap = new StateWrap(SessionId, LicProv, true);
		bool success = wrap.Engine.DeleteInUserTOC(request.Name, true, out string message);
		return await Task.FromResult(new GenericResponse(success ? 0 : 1, message));
	}

	async Task<ActionResult<XlsxResponse>> QuickUpdateReportImpl(QuickUpdateRequest request)
	{
		var watch = new Stopwatch();
		watch.Start();
		using var wrap = new StateWrap(SessionId, LicProv, true);
		bool result = wrap.Engine.QuickEdit(request.ShowFreq, request.ShowColPct, request.ShowRowPct, request.ShowSig, request.Filter);
		LogInfo(262, "QuickEdit {Freq} {Col} {Row} {Sig} {Filter}", request.ShowFreq, request.ShowColPct, request.ShowColPct, request.ShowSig, request.Filter);
		return await MakeXlsxAndUpload(wrap, "Quick");
	}

	async Task<ActionResult<GenericResponse>> SaveReportImpl(SaveReportRequest request)
	{
		using var wrap = new StateWrap(SessionId, LicProv, true);
		bool success = wrap.Engine.SaveTableUserTOC(request.Name, request.Sub, true);
		LogInfo(260, "SaveReport {Name}+{Sub}", request.Name, request.Sub);
		return await Task.FromResult(new GenericResponse(0, request.Name));
	}

	async Task<ActionResult<XlsxResponse>> GenTabExcelBlobImpl([FromBody] GenTabRequest request)
	{
		using var wrap = new StateWrap(SessionId, LicProv, false);
		wrap.Engine.GenTab(request.Name, request.Top, request.Side, request.Filter, request.Weight, request.SProps, request.DProps);
		var resp = await MakeXlsxAndUpload(wrap, "ReportGenTabExcelBlob");
		return await Task.FromResult(resp);
	}

	async Task<ActionResult<string[]>> GenTabHtmlImpl(GenTabHtmlRequest request)
	{
		using var wrap = new StateWrap(SessionId, LicProv, true);
		string report = wrap.Engine.GenTabAsHTML(request.Top, request.Side, request.Filter, request.Weight, request.CaseFilter);
		var lines = CommonUtil.ReadStringLines(report).ToArray();
		LogInfo(231, "GenTabHtml({Top},{Side},{Filter},{Weight},{CaseFilter) -> #{Length})", request.Top, request.Side, request.Filter, request.Weight, request.CaseFilter, lines.Length);
		return await Task.FromResult(lines);
	}

	async Task<ActionResult<string>> GenTabHtmlJoinedImpl(GenTabHtmlRequest request)
	{
		using var wrap = new StateWrap(SessionId, LicProv, true);
		string report = wrap.Engine.GenTabAsHTML(request.Top, request.Side, request.Filter, request.Weight, request.CaseFilter);
		LogInfo(231, "GenTabHtmlJoined({Top},{Side},{Filter},{Weight},{CaseFilter) -> #{Length})", request.Top, request.Side, request.Filter, request.Weight, request.CaseFilter, report.Length);
		return await Task.FromResult(report);
	}

	async Task<ActionResult<GenNode[]>> AxisSyntaxToNodesImpl(string syntax)
	{
		using var wrap = new StateWrap(SessionId, LicProv, true);
		var nodes = wrap.Engine.AxisSyntaxToNodes(syntax);
		return await Task.FromResult(nodes);
	}

	async Task<ActionResult<string>> AxisNodesToSyntaxImpl(GenNode[] nodes)
	{
		using var wrap = new StateWrap(SessionId, LicProv, true);
		string syntax = wrap.Engine.AxisNodesToSyntax(nodes);
		return await Task.FromResult(syntax);
	}

	async Task<ActionResult<string?[]>> GetCurrentSyntaxImpl()
	{
		using var wrap = new StateWrap(SessionId, LicProv, true);
		string joined = wrap.Engine.CurrentSyntax();
		var lines = CommonUtil.ReadStringLines(joined).ToArray();
		static string? Reduce(string s) => string.IsNullOrEmpty(s) ? null : s;
		// The lines in the format Key=Value are parsed and the values are returned in an array in the following Key index order.
		string[] Keys = { "Top", "Side", "Filter", "Weight", "Case" };
		var query = lines
			.Select(x => Regex.Match(x, @"^(\w+)=(.*)"))
			.Where(m => m.Success)
			.Select(m => new { Key = m.Groups[1].Value, Val = m.Groups[2].Value, Ix = Array.IndexOf(Keys, m.Groups[1].Value) })
			.Where(x => x.Ix >= 0);
		string?[] syns = new string[Keys.Length];
		foreach (var tup in query)
		{
			syns[tup.Ix] = Reduce(tup.Val);
		}
		return await Task.FromResult(syns);
	}

	async Task<ActionResult<string>> ValidateSyntaxImpl(string syntax)
	{
		using var wrap = new StateWrap(SessionId, LicProv, true);
		string message = wrap.Engine.ValidateSyntax(syntax);
		if (message == "ok") return null;   // <========================= NOTE THAT 'ok' IS HARD_CODED IN THE CARBON METHOD =========================
		return await Task.FromResult(message);
	}

	async Task<ActionResult<string[]>> FormatImpl(XOutputFormat format)
	{
		using var wrap = new StateWrap(SessionId, LicProv, true);
		string report = wrap.Engine.TableAsFormat(format);
		string[] lines = ServiceUtility.ListStringLines(report).ToArray();
		return await Task.FromResult(lines);
	}
}
