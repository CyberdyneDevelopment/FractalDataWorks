namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>Writes a <see cref="IAuditFindingKind"/> as its name and reads it back through <c>AuditFindingKinds.ByName</c>.</summary>
public sealed class AuditFindingKindJsonConverter : Fdw.Roslyn.Commands.Json.TypeOptionNameJsonConverter<IAuditFindingKind>
{
    /// <inheritdoc />
    protected override IAuditFindingKind ByName(string name) => AuditFindingKinds.ByName(name);
}
