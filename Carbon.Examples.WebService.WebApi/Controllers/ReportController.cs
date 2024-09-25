using System;
using System.Linq;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Carbon.Examples.WebService.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RCS.Carbon.Shared;
using RCS.Carbon.Tables;

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

	async Task<ActionResult<XlsxResponse>> GenTabExcelBlobImpl([FromBody] GenTabRequest request)
	{
		using var wrap = new StateWrap(SessionId, LicProv, false);
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
		string[] Keys = { "Top", "Side", "Filter", "Weight" };
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
		if (message == "ok") return null;   // <========================= NOTE THAT 'ok' IS HARD_CODED IN TEH CARBON METHOD =========================
		return await Task.FromResult(message);
	}
}
