namespace Fdw.Sql.Commands.Project.Translators;

/// <summary>Detailed metadata + definition script for one SQL object.</summary>
public sealed record SqlObjectDetail(string Name, string FullName, string Kind, string? Definition);
