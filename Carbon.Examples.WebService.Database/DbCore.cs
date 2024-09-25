using System.Net;
using System.Text.RegularExpressions;
using Azure;
using Azure.Data.Tables;

namespace Carbon.Examples.WebService.Database;

/// <summary>
/// Implements a very simple general purpose database for put, get, list and delete of strings using Azure Table Storage.
/// </summary>
public sealed partial class DbCore
{
	public DbCore(string connectionString, string tableName)
	{
		ArgumentNullException.ThrowIfNull(connectionString, nameof(connectionString));
		ArgumentNullException.ThrowIfNull(tableName, nameof(tableName));
		ConnectionString = connectionString;
		TableName = tableName;
	}

	public string ConnectionString { get; }
	public string TableName { get; }
	TableClient? _client;

	public async Task Put(DbRow row)
	{
		await Put(row.Key1, row.Key2, row.Value);
	}

	public async Task<bool> Put(string key1, string key2, string? value)
	{
		ArgumentNullException.ThrowIfNull(key1, nameof(key1));
		ArgumentNullException.ThrowIfNull(key2, nameof(key2));
		var client = await GetTableClientAsync();
		string realkey1 = Encode(key1);
		string realkey2 = Encode(key2);
		NullableResponse<TableEntity> response = await client.GetEntityIfExistsAsync<TableEntity>(realkey1, realkey2);
		if (response.HasValue)
		{
			if (value == null)
			{
				// An existing value going null is silently deleted.
				await client.DeleteEntityAsync(Encode(key1), Encode(key2));
			}
			else
			{
				// An existing value is updated.
				response.Value!["Value"] = value;
				await client.UpsertEntityAsync(response.Value!);
			}
			return true;
		}
		else
		{
			if (value == null)
			{
				// Nothing to do. No rows with null values.
			}
			else
			{
				// A new row is created for the value.
				var row = new TableEntity(Encode(key1), Encode(key2));
				await client.UpsertEntityAsync(row);
			}
			return false;
		}
	}

	public async Task<string?> Read(string key1, string key2)
	{
		ArgumentNullException.ThrowIfNull(key1, nameof(key1));
		ArgumentNullException.ThrowIfNull(key2, nameof(key2));
		var client = await GetTableClientAsync();
		NullableResponse<TableEntity> response = await client.GetEntityIfExistsAsync<TableEntity>(Encode(key1), Encode(key2));
		if (response.HasValue)
		{
			return response.Value!.GetString("Value");
		}
		return null;
	}

	public async IAsyncEnumerable<DbRow> ListRows(bool returnValues = false)
	{
		var client = await GetTableClientAsync();
		await foreach (var row in client.QueryAsync<TableEntity>())
		{
			yield return new DbRow(Decode(row.PartitionKey), Decode(row.RowKey), returnValues ? row.GetString("Value") : null);
		}
	}

	public async Task<bool> Delete(string key1, string key2)
	{
		ArgumentNullException.ThrowIfNull(key1, nameof(key1));
		ArgumentNullException.ThrowIfNull(key2, nameof(key2));
		var client = await GetTableClientAsync();
		Response response = await client.DeleteEntityAsync(Encode(key1), Encode(key2));
		return response.Status == (int)HttpStatusCode.NoContent;
	}

	async Task<TableClient> GetTableClientAsync()
	{
		if (_client == null)
		{
			var tsc = new TableServiceClient(ConnectionString);
			_client = tsc.GetTableClient(TableName);
			await _client.CreateIfNotExistsAsync();
		}
		return _client;
	}

	static string Encode(string sourceKey) => Regex.Replace(sourceKey, "[\x00-\x1f\x7f-\x9f\\\\/#?%+]", m => $"+{(int)m.Value[0]:X2}");

	static string Decode(string encodedKey) => Regex.Replace(encodedKey, "(\\+[0-9A-F]{2})", m => ((char)(Convert.ToByte(m.Groups[0].Value.Substring(1), 16))).ToString());
}
