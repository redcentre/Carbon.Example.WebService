using System;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace Carbon.Examples.WebService.WebApi;

public class TextPlainInputFormatter : TextInputFormatter
{
	public TextPlainInputFormatter()
	{
		SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(MediaTypeNames.Text.Plain));
		SupportedEncodings.Add(Encoding.UTF8);
	}

	protected override bool CanReadType(Type type) => true;

	public override bool CanRead(InputFormatterContext context) => true;

	public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
	{
		var request = context.HttpContext.Request;
		using var streamReader = context.ReaderFactory(request.Body, encoding);
		string content = await streamReader.ReadToEndAsync();
		object? model = TextConvert.Deserialize(context.ModelType, content, false);
		return await InputFormatterResult.SuccessAsync(model);
	}
}