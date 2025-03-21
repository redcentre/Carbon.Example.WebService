using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Carbon.Examples.WebService.Common;
using Carbon.Examples.WebService.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RCS.Carbon.Licensing.Shared;

namespace Carbon.Examples.WebService.WebApi.Controllers;

/// <ignore/>
public partial class DbController : ServiceControllerBase
{
	readonly DbCore _core;

	public DbController(ILoggerFactory logfac, IConfiguration config, ILicensingProvider licprov)
		: base(logfac, config, licprov)
	{
		_core = new DbCore(Config["CarbonApi:ApplicationStorageConnect"]!, Config["CarbonApi:DatabaseTableName"]!);
	}

	/// <summary>
	/// Puts a string value in the simple database.
	/// </summary>
	/// <param name="key1">Database primary key.</param>
	/// <param name="key2">Database secondary key.</param>
	/// <param name="value">The value to store in the database. The request body is processed as plain text. No parsing or interpretation of the text body value is performed.</param>
	/// <response code="204">The value was added or replaced into the database. There is no response body data.</response>
	/// <include file='DocInclude.xml' path='doc/members[@name="Auth403"]/*'/>
	[HttpPost]
	[Route("db/{key1}/{key2}")]
	[AuthFilter]
	[Consumes(MediaTypeNames.Text.Plain)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden, MediaTypeNames.Application.Json)]
	public async Task<IResult> DbPut([FromRoute] string key1, [FromRoute] string key2, /* Note that [FromBody] cannot be used */ string value)
	{
		bool updated = await _core.Put(key1, key2, value);
		return TypedResults.NoContent();
	}

	/// <summary>
	/// Reads a string from the simple database.
	/// </summary>
	/// <param name="key1">Database primary key.</param>
	/// <param name="key2">Database secondary key.</param>
	/// <response code="200">The response body contains the plain string value. The response is always plain text.</response>
	/// <include file='DocInclude.xml' path='doc/members[@name="Auth403"]/*'/>
	/// <response code="404">No database row was found with the specified keys.</response>
	[HttpGet]
	[Route("db/{key1}/{key2}")]
	[AuthFilter]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK, MediaTypeNames.Text.Plain)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden, MediaTypeNames.Application.Json)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound, MediaTypeNames.Application.Json)]
	public async Task<ActionResult> DbRead([FromRoute] string key1, [FromRoute] string key2)
	{
		string? value = await _core.Read(key1, key2);
		if (value == null) return NotFound(new ErrorResponse(404, $"Read failed. No database row was found with keys [{key1},{key2}]."));
		return new ContentResult() { Content = value, ContentType = MediaTypeNames.Text.Plain, StatusCode = StatusCodes.Status200OK };
	}

	/// <summary>
	/// Lists keys in the simple database, optionally returning the values
	/// </summary>
	/// <param name="includeValues">Specifity <c>true</c> to return the values with the keys. The default is <c>false</c>.</param>
	/// <response code="200">The response body contains a serialized array of <c>DbRow</c>.</response>
	/// <include file='DocInclude.xml' path='doc/members[@name="Auth403"]/*'/>
	[HttpGet]
	[Route("db/list")]
	[AuthFilter]
	[Produces("application/json", "text/xml")]
	[ProducesResponseType(typeof(DbRow[]), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
	public async Task<DbRow[]> DbGet([FromQuery] bool includeValues = false)
	{
		return await _core.ListRows(includeValues).ToArrayAsync();
	}

	/// <summary>
	/// Deletes a row from the simple database.
	/// </summary>
	/// <param name="key1">Database primary key.</param>
	/// <param name="key2">Database secondary key.</param>
	/// <response code="204">The row was deleted. There is no response body data.</response>
	/// <include file='DocInclude.xml' path='doc/members[@name="Auth403"]/*'/>
	/// <response code="404">No database row was found with the specified keys.</response>
	[HttpDelete]
	[Route("db/{key1}/{key2}")]
	[AuthFilter]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden, MediaTypeNames.Application.Json)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound, MediaTypeNames.Application.Json)]
	public async Task<ActionResult> DbDelete([FromRoute] string key1, [FromRoute] string key2)
	{
		bool deleted = await _core.Delete(key1, key2);
		if (deleted) return NoContent();
		return NotFound(new ErrorResponse(404, $"Delete failed. No database row was found with keys [{key1},{key2}]."));
	}
}
