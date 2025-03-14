using System;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Carbon.Examples.WebService.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RCS.Carbon.Licensing.Shared;
using RCS.Carbon.Shared;
using RCS.Carbon.Tables;
using RCS.Carbon.Variables;
using tab = RCS.Carbon.Tables;

namespace Carbon.Examples.WebService.WebApi.Controllers;

/// <ignore/>
[ApiController]
[Route("python")]
public class PythonController : ServiceControllerBase
{
	/// <ignore/>
	public PythonController(ILoggerFactory logfac, IConfiguration config, ILicensingProvider licprov)
		: base(logfac, config, licprov)
	{
	}

	/// <summary>
	/// Generates a crosstab report for Python clients using the pandas library.
	/// </summary>
	/// <response code="200">A string of Json in a format suitable for processing by the pandas library.</response>
	/// <remarks>
	/// Note that this endpoint cannot be tested via the Swagger interface. The JSON in the request must be written into the body of the request 
	/// for the POST method call. Python clients can pass the JSON body via the <c>requests.post</c> method call. .NET clients can use Framework 
	/// provided web client classes to issue POST requests with JSON bodies.
	/// </remarks>
	[HttpPost]
	[Route("crosstab/alphacodes")]
	[Produces("application/json")]
	public async Task<IActionResult> PandasDataframe()
	{
		string body;
		using (var reader = new StreamReader(HttpContext.Request.Body))
		{
			body = await reader.ReadToEndAsync();
		}
		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
		// The type/shape of each root property is determined
		// and it is set in the top or side incoming data.
		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
		IncomingData topData = new();
		IncomingData sideData = new();
		XDisplayProperties dprops = new XDisplayProperties();

		JsonDocument doc = JsonDocument.Parse(body);
		JsonProperty[] rootprops = doc.RootElement.EnumerateObject().ToArray();
		foreach (var prop in rootprops)
		{
			var upname = prop.Name.ToUpperInvariant();
			if (upname == "PROPS")
			{
				if (prop.Value.ValueKind == JsonValueKind.String)
				{
					dprops.Deserialize(prop.Value.GetString());
				}
				else if (prop.Value.ValueKind != JsonValueKind.Null)
				{
					return BadRequest(new ErrorResponse(303, $"Root property '{prop.Name}' is not a string value"));
				}
			}
			else if (upname == "TOP" || upname == "SIDE")
			{
				IncomingData currData = upname == "TOP" ? topData : sideData;
				if (prop.Value.ValueKind != JsonValueKind.Array)
				{
					return BadRequest(new ErrorResponse(301, $"Root property '{prop.Name}' must be an array"));
				}
				JsonValueKind[] kinds = prop.Value.EnumerateArray().Select(v => v.ValueKind).ToArray();
				if (kinds.All(v => v == JsonValueKind.String))
				{
					// ┌──────────────────────────────────────────────────────┐
					// │  Species 1 - Array of string                         │
					// └──────────────────────────────────────────────────────┘
					currData.Labels = prop.Value.EnumerateArray().Select(v => new string[] { v.GetString()! }).ToArray();
				}
				else if (kinds.All(v => v == JsonValueKind.Number))
				{
					// ┌──────────────────────────────────────────────────────┐
					// │  Species 2 - Array of double as strings              │
					// └──────────────────────────────────────────────────────┘
					currData.Labels = prop.Value.EnumerateArray().Select(v => new string[] { v.GetDouble().ToString() }).ToArray();
				}
				else if (kinds.All(v => v == JsonValueKind.Array))
				{
					var tups = prop.Value.EnumerateArray().Select(v => new { Len = v.GetArrayLength(), Kinds = v.EnumerateArray().Select(x => x.ValueKind).ToArray() }).ToArray();
					if (tups.All(t => t.Len == 2 && t.Kinds[0] == JsonValueKind.String && t.Kinds[1] == JsonValueKind.Number))
					{
						// ┌──────────────────────────────────────────────────────┐
						// │  Species 4 - Array of (code,inc)                     │
						// └──────────────────────────────────────────────────────┘
						currData.Labels = prop.Value.EnumerateArray().Select(v => new string[] { v[0].GetString()! }).ToArray();
						currData.Increments = prop.Value.EnumerateArray().Select(v => new double[] { v[1].GetDouble()! }).ToArray();
					}
					else if (tups.All(t => t.Kinds.All(k => k == JsonValueKind.String)))
					{
						// ┌──────────────────────────────────────────────────────┐
						// │  Species 3 - Jagged string array                     │
						// └──────────────────────────────────────────────────────┘
						currData.Labels = prop.Value.EnumerateArray().Select(v => v.EnumerateArray().Select(v => v.GetString()!).ToArray()).ToArray();
					}
					else if (tups.All(t => t.Kinds.All(k => k == JsonValueKind.Array && prop.Value.EnumerateArray().SelectMany(v => v.EnumerateArray()).All(x => x.GetArrayLength() == 2 && x[0].ValueKind == JsonValueKind.String && x[1].ValueKind == JsonValueKind.Number))))
					{
						// ┌──────────────────────────────────────────────────────┐
						// │  Species 5 - Jagged array of (code,inc)              │
						// └──────────────────────────────────────────────────────┘
						var species5 = prop.Value.EnumerateArray().Select(v => v.EnumerateArray().Select(x => new { Code = x[0].GetString()!, Val = x[1].GetDouble() }).ToArray()).ToArray();
						currData.Labels = species5.Select(x => x.Select(y => y.Code).ToArray()).ToArray();
						currData.Increments = species5.Select(x => x.Select(y => y.Val).ToArray()).ToArray();
					}
					else
					{
						return BadRequest(new ErrorResponse(302, $"Root property '{prop.Name}' is not in a supported format"));
					}
				}
			}
			else
			{
				return BadRequest(new ErrorResponse(300, $"Property name '{prop.Name}' not supported"));
			}
		}
		var engine = new tab.CrossTabEngine(LicProv);
		tab.PandasRawData prd = engine.GenTabAsPandas(topData, sideData, dprops);
		var dict = prd.ToShape1();
		string respjson = JsonSerializer.Serialize(dict, new JsonSerializerOptions() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
		LogInfo(410, "Pandas {Length}", respjson.Length);
		// NOTE: It took two hours of suffering to discover that the following
		// line is the correct way of return a plain string of JSON in the body.
		return new ContentResult() { Content = respjson, ContentType = "application/json", StatusCode = 200 };
	}

	/// <summary>
	/// Generates a crosstab report using variables stored in a Ruby job.
	/// </summary>
	/// <param name="request"></param>
	/// <response code="200">A string of Json in a format suitable for processing by the pandas library.</response>
	/// <response code="400">Incorrect parameters in the request.</response>
	/// <remarks>
	/// <para>
	/// This endpoint is intended for running intermitted ad-hoc reports against data that is stored in an existing
	/// cloud job. Each call is independent and inefficient because the full API call cycle of Login &#x2192; OpenJob
	/// &#x2192; GenTab &#x2192; CloseJob &#x2192; Logoff is performed internally for each call. This endpoint is not
	/// designed to be called many times in rapid succession. A new set of endpoints can be created if required for
	/// Python clients who need to generate many successive reports.
	/// </para>
	/// <para>
	/// If the request <c>id</c> and <c>password</c> credentials are null then the free account is used, which
	/// is acceptable for demonstrations and evaluation by unlicensed customers.
	/// </para>
	/// <para>
	/// The <c>FormatType</c> parameter is an integer that can be 1, 2 or 3. Each format produces JSON with a slightly
	/// different shape. Format 1 is the default if the value is out of range.
	/// </para>
	/// </remarks>
	[HttpPost]
	[Route("crosstab/gentab")]
	[Produces("application/json")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> PandasGenTab(GenTabPythonRequest request)
	{
		if ((request.Id != null && request.password == null) ||
			(request.CustomerName == null) ||
			(request.JobName == null) ||
			(request.Top == null) ||
			(request.Side == null))
		{
			return new JsonResult(new ErrorResponse(666, "Mandatory parameter values are not specified in the request")) { StatusCode = StatusCodes.Status400BadRequest };
		}
		if (request.FormatType < 1 || request.FormatType > 3)
		{
			request.FormatType = 1;
		}
		LicenceInfo lic;
		var engine = new CrossTabEngine(LicProv);
		try
		{
			if (request.Id == null)
			{
				lic = await engine.GetFreeLicence("TESTING");
			}
			else
			{
				lic = await engine.GetLicenceId(request.Id, request.password!, request.SkipCache);
			}
		}
		catch (Exception ex)
		{
			return new JsonResult(new ErrorResponse(666, $"Incorrect credentials: {ex.Message}")) { StatusCode = StatusCodes.Status400BadRequest };
		}
		try
		{
			engine.OpenJob(request.CustomerName, request.JobName);
		}
		catch (CarbonException ex)
		{
			return new JsonResult(new ErrorResponse(666, $"Open job failed: {ex.Message}"));
		}
		PandasRawData? raw = null;
		try
		{
			var sprops = new XSpecProperties();
			var dprops = new XDisplayProperties();
			dprops.Cells.Frequencies.Visible = request.ColumnPercents != true;
			dprops.Cells.RowPercents.Visible = request.ColumnPercents == true;
			dprops.Cells.ColumnPercents.Visible = false;
			dprops.Output.Format = XOutputFormat.None;
			engine.GenTab(null, request.Top, request.Side, request.Filter, request.Weight, sprops, dprops);
			raw = engine.GetTabAsPandas();
		}
		catch (CarbonException ex)
		{
			return new JsonResult(new ErrorResponse(666, $"GenTab failed: {ex.Message}")) { StatusCode = StatusCodes.Status400BadRequest };
		}
		object dict;
		if (request.FormatType == 1)
		{
			dict = raw.ToShape1();
		}
		else if (request.FormatType == 2)
		{
			dict = raw.ToShape2();
		}
		else
		{
			dict = raw.ToShape3();
		}
		string respjson = JsonSerializer.Serialize(dict, new JsonSerializerOptions() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
		LogInfo(420, "PandasGenTab{Format} {Length}", request.FormatType, respjson.Length);
		engine.CloseJob();
		if (request.Id != null)
		{
			await engine.LogoutId(request.Id);
		}
		return new ContentResult() { Content = respjson, ContentType = "application/json", StatusCode = StatusCodes.Status200OK };
	}
}
/*
 * Shape of the JSON from the raw output formats
------------------------- 1
 {
  "15-25": {
    "NE": "41",
    "SE": "43",
    "SW": "48",
    "NW": "40",
    "East": "84",
    "West": "88",
    "North": "81",
    "South": "91"
  },
------------------------- 2
{
  "15-25": [
    "41",
    "43",
    "48",
    "40",
    "84",
    "88",
    "81",
    "91"
  ],
------------------------- 3
{
  "Age": [
    "NE",
    "SE",
    "SW",
    "NW",
    "East",
    "West",
    "North",
    "South"
  ],
  "15-25": [
    "41",
    "43",
    "48",
    "40",
    "84",
    "88",
    "81",
    "91"
  ],
 */
