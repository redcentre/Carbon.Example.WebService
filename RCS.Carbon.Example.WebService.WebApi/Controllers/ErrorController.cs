using RCS.Carbon.Example.WebService.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RCS.Licensing.Provider.Shared;

namespace RCS.Carbon.Example.WebService.WebApi.Controllers;

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
			// It is expected that most unhandled errors will arrive here.
			// Respond with the typical status 500 and a possibly useful message.
			HttpContext.Items["ErrorType"] = handler.Error.GetType().Name;
			HttpContext.Items["ErrorMessage"] = handler.Error.Message;
			HttpContext.Items["ErrorStack"] = handler.Error.StackTrace;
			HttpContext.Items["ErrorPath"] = handler.Path;
			return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse(1, $"{handler.Error.Message}"));
		}
		const string BadMessage = "The error handler could not find an error feature to provide error details";
		//Logger.LogError(901, null, BadMessage);
		HttpContext.Items["ErrorMessage"] = BadMessage;
		return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse(2, BadMessage));
	}
}