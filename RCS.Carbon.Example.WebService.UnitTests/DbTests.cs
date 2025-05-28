using System.Threading.Tasks;
using RCS.Carbon.Example.WebService.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RCS.Carbon.Example.WebService.UnitTests
{
	[TestClass]
	public sealed class DbTests : TestBase
    {
		[TestMethod]
		public async Task T010_Puts()
		{
			var db = MakeDb();
			bool updated = await db.Put("Key1", "Key21", "Some value");
			Assert.IsFalse(updated);
			updated = await db.Put("Key1", "Key21", "THIS IS A NEW VALUE!!");
			Assert.IsTrue(updated);
			updated = await db.Put("Key1", "Key21", null);
			Assert.IsTrue(updated);
			updated = await db.Put("Key1", "Key21", null);
			Assert.IsFalse(updated);
			await db.Put("Key1", "Key22", "Another value");
			await db.Put("WeirdKey\x00АБВГД", "Weird/Key2", "I hope the keys are encoded");
		}

		[TestMethod]
		public async Task T020_List()
		{
			var db = MakeDb();
			await foreach (var row in db.ListRows())
			{
				Trace($"{row.Key1}|{row.Key2} = {row.Value}");
			}
		}

		[TestMethod]
		public async Task T030_Delete()
		{
			var db = MakeDb();
			await db.Delete("Key1", "Key22");
		}

		static DbCore MakeDb() => new DbCore("DefaultEndpointsProtocol=https;AccountName=carbonapi;AccountKey=;TableEndpoint=https://carbonapi.table.core.windows.net/;", "Database1");
	}
}
