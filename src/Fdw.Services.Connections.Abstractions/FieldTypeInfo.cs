namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Describes a single field type supported by a connection type, including its native database type name
/// and a human-readable display name.
/// </summary>
/// <param name="Name">The canonical type name used in code (e.g. "varchar").</param>
/// <param name="DbTypeName">The native database type expression (e.g. "varchar(max)").</param>
/// <param name="DisplayName">The human-readable label shown in the UI (e.g. "Text (varchar)").</param>
// Why: pure positional record (DTO), auto-generated properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record FieldTypeInfo(string Name, string DbTypeName, string DisplayName);
