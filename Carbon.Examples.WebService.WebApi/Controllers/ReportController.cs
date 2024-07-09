using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Xml.Linq;
using Carbon.Examples.WebService.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RCS.Carbon.Licensing.Shared;
using RCS.Carbon.Shared;
using RCS.Carbon.Tables;

namespace Carbon.Examples.WebService.WebApi.Controllers;

/// <ignore/>
[ApiController]
[Route("report")]
[TypeFilter(typeof(GeneralActionFilterAttribute))]
public partial class ReportController : ServiceControllerBase
{
	/// <ignore/>
	public ReportController(ILoggerFactory logfac, IConfiguration config, ILicensingProvider licprov)
		: base(logfac, config, licprov)
	{
	}

	/// <summary>
	/// Generates a crosstab report as plain text in different formats.
	/// </summary>
	/// <param name="format">Must be set to one of the enumeration values: TSV, CSV, SSV, XML, HTML, OXT, OXTNums, MultiCube.
	/// Only these report formats can be represented as plain text and returned by this endpoint.</param>
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
	public async Task<ActionResult<string>> ReportGenTabText([FromRoute] XOutputFormat format, [FromBody] GenTabRequest request)
	{
		XOutputFormat[] formats = new XOutputFormat[]
		{
			XOutputFormat.TSV,
			XOutputFormat.CSV,
			XOutputFormat.SSV,
			XOutputFormat.XML,
			XOutputFormat.HTML,
			XOutputFormat.OXT,
			XOutputFormat.OXTNums,
			XOutputFormat.MultiCube
		};
		if (!formats.Contains(format))
		{
			return BadRequest(new ErrorResponse(600, $"Report gentab format '{request.DProps.Output.Format}' is not acceptable."));
		}
		request.DProps.Output.Format = format;
		using var wrap = new StateWrap(SessionId, LicProv, false);
		string report = wrap.Engine.GenTab(request.Name, request.Top, request.Side, request.Filter, request.Weight, request.SProps, request.DProps);
		LogInfo(510, "GenTab({Format},{Top},{Side},{Filter},{Weight})", request.DProps.Output.Format, request.Top, request.Side, request.Filter, request.Weight);
		var result = new ContentResult() { Content = report, ContentType = MediaTypeNames.Text.Plain, StatusCode = StatusCodes.Status200OK };
		return await Task.FromResult(result);
	}

	/// <summary>
	/// Generates a crosstab report as an XML fragment.
	/// </summary>
	/// <param name="request">A serialized <c>GenTabRequest</c> provided in the request body.</param>
	/// <response code="200">The string body of a crosstab report as an XML fragment.</response>
	/// <include file='DocInclude.xml' path='doc/members[@name="Auth403"]/*'/>
	/// <remarks>The response content-type is always text/xml. This endpoint is experimental and the optimal shape of the
	/// returned XML has not been determined yet.</remarks>
	[HttpPost]
	[Route("gentab/xml")]
	[AuthFilter]
	[Produces(MediaTypeNames.Application.Json, MediaTypeNames.Text.Xml)]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK, MediaTypeNames.Text.Xml)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
	public async Task<ActionResult<string>> ReportGenTabXml([FromBody] GenTabRequest request)
	{
		using var wrap = new StateWrap(SessionId, LicProv, false);
		XDocument doc = wrap.Engine.GenTabAsXML(request.Name, request.Top, request.Side, request.Filter, request.Weight, request.SProps, request.DProps);
		var result = new ContentResult() { Content = doc.ToString(), ContentType = MediaTypeNames.Application.Xml, StatusCode = StatusCodes.Status200OK };
		return await Task.FromResult(result);
	}

	/// <summary>
	/// Generates a crosstab report in an Excel workbook, stores it in a publicly accessible Azure Blob and returns the Uri of the Blob.
	/// </summary>
	/// <param name="request">A serialized <c>GenTabRequest</c> provided in the request body.</param>
	/// <response code="200">A serialized <c>XlsxResponse</c> containing the Uri and attributes of the generated Excel workbook Blob.</response>
	/// <include file='DocInclude.xml' path='doc/members[@name="Auth403"]/*'/>
	[HttpPost]
	[Route("gentab/excel/blob")]
	[AuthFilter]
	[Produces(MediaTypeNames.Application.Json)]
	[ProducesResponseType(typeof(XlsxResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
	public async Task<ActionResult<XlsxResponse>> ReportGenTabExcelBlob([FromBody] GenTabRequest request)
	{
		using var wrap = new StateWrap(SessionId, LicProv, false);
		var resp = await MakeXlsxAndUpload(wrap, "ReportGenTabExcelBlob");
		return await Task.FromResult(resp);
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
}
