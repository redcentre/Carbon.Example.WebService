namespace Carbon.Examples.WebService.Database;

public sealed class DbRow
{
	public DbRow()
	{
	}

	public DbRow(string key1, string key2, string? value)
	{
		Key1 = key1;
		Key2 = key2;
		Value = value;
	}
	public string Key1 { get; }
	public string Key2 { get; }
	public string? Value { get; }
}
