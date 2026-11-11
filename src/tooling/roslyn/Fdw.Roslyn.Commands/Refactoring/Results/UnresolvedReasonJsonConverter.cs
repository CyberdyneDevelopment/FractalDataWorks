namespace Fdw.Roslyn.Commands.Refactoring.Results;

/// <summary>Writes a <see cref="IUnresolvedReason"/> as its name and reads it back through <c>UnresolvedReasons.ByName</c>.</summary>
public sealed class UnresolvedReasonJsonConverter : Fdw.Roslyn.Commands.Json.TypeOptionNameJsonConverter<IUnresolvedReason>
{
    /// <inheritdoc />
    protected override IUnresolvedReason ByName(string name) => UnresolvedReasons.ByName(name);
}
