namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// What an audit finding is about.
/// </summary>
public enum AuditFindingKind
{
    /// <summary>The project references an assembly the guide forbids.</summary>
    ForbiddenReference,
    /// <summary>The project lacks an assembly the guide requires.</summary>
    MissingReference,
    /// <summary>The type leaves a member of the abstraction unimplemented.</summary>
    UnimplementedMember,
}
