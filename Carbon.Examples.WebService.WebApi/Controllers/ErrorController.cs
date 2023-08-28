using System.Diagnostics;
using Carbon.Examples.WebService.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RCS.Carbon.Licensing.Shared;

namespace Carbon.Examples.WebService.WebApi.Controllers;

/// <ignore/>
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[TypeFilter(typeof(GeneralActionFilterAttribute))]
public class ErrorController : ServiceControllerBase
{
	/// <ignore/>
	public ErrorController(ILoggerFactory logfac, IConfiguration config, ILicensingProvider licprov)
		: base(logfac, config, licprov)
	{
	}

	/// <ignore/>
	[Route("error")]
	public IActionResult Error()
	{
		IExceptionHandlerFeature? handler = HttpContext.Features.Get<IExceptionHandlerFeature>();
		if (handler != null)
		{
			int count = (int)HttpContext.Items["count"]!;
			Trace.WriteLine($"{count} {HttpContext.Response.StatusCode} {handler.Error}");
			// It is expected that most unhandled errors will arrive here.
			// Respond with the typical status 500 and a possibly useful message.
			Logger.LogError(900, handler.Error, "{RequestSequence} Global error handler", RequestSequence);
			return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse(1, $"{handler.Error.Message}"));
		}
		const string BadMessage = "{RequestSequence} The error handler could not find an error feature to provide error details";
		Logger.LogError(901, null, BadMessage, RequestSequence);
		return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse(2, $"Unidentified Error: {BadMessage}"));
	}
}