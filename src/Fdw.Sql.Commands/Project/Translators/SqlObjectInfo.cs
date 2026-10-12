namespace Fdw.Sql.Commands.Project.Translators;

/// <summary>Lightweight object descriptor returned from ListObjects.</summary>
public sealed record SqlObjectInfo(string Name, string Schema, string Kind, string FullName);
