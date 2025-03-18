using System;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace Carbon.Examples.WebService.WebApi;

public class TextPlainOutputFormatter : TextOutputFormatter
{
	public TextPlainOutputFormatter()
	{
		SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(MediaTypeNames.Text.Plain));
		SupportedEncodings.Add(Encoding.UTF8);
		SupportedEncodings.Add(Encoding.ASCII);
	}

	protected override bool CanWriteType(Type? type) => true;

	public override bool CanWriteResult(OutputFormatterCanWriteContext context) => true;

	public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
	{
		string body = TextConvert.SerializeObject(context.Object);
		await context.HttpContext.Response.WriteAsync(body, selectedEncoding);
	}
}
