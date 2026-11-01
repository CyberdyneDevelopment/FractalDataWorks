namespace Fdw.Services.Settings;

/// <summary>Metadata describing a well-known server setting.</summary>
/// <param name="DataType">The value's data type (e.g. "String", "Int32", "Boolean").</param>
/// <param name="Description">A human-readable description of the setting.</param>
public readonly record struct SettingDefinition(string DataType, string Description);
