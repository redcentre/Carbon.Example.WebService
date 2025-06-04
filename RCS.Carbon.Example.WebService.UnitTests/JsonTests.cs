using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCS.Carbon.Example.WebService.Common.DTO;

namespace RCS.Carbon.Example.WebService.UnitTests
{
	[TestClass]
	public sealed class JsonTests : TestBase
    {
		[TestMethod]
		public void T010_Deserialize_JobOpen()
		{
			string json = File.ReadAllText(@"K:\dev_rcs\Silver\_support\OpenJobResponse.json");
			var opts = new JsonSerializerOptions() { WriteIndented = true, PropertyNameCaseInsensitive = true };
			opts.Converters.Add(new JsonStringEnumConverter());
			var response = JsonSerializer.Deserialize<OpenCloudJobResponse>(json, opts);
			string json2 = JsonSerializer.Serialize(response, opts);
			Trace(json2);
		}
	}
}
