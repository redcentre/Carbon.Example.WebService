namespace Carbon.Examples.WebService.Database;

public sealed class DbRow(string key1, string key2, string? value)
{
	public string Key1 { get; } = key1;
	public string Key2 { get; } = key2;
	public string? Value { get; } = value;
}
