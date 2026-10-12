using System.Text.Json.Serialization;

namespace Fdw.Roslyn.Commands.Refactoring.Results;

/// <summary>
/// Explains why an <c>&lt;inheritdoc/&gt;</c> site could not be expanded — these are the
/// true MA0196 candidates that need explicit documentation written by hand.
/// </summary>
#pragma warning disable FDW017 // Result-DTO value enum — TypeCollection not applicable here
[JsonConverter(typeof(JsonStringEnumConverter<UnresolvedReason>))]
public enum UnresolvedReason
{
    /// <summary>
    /// The member has no overridden member and implements no interface member, so there is
    /// nothing for <c>&lt;inheritdoc/&gt;</c> to inherit documentation from.
    /// </summary>
    NoBaseMember,

    /// <summary>
    /// A base member (override or interface member) exists, but it carries no XML documentation
    /// to inherit. Commonly a BCL or referenced-assembly member whose XML doc file did not ship.
    /// </summary>
    BaseHasNoDocs,

    /// <summary>
    /// The inheritance chain refers back to itself (for example an explicit
    /// <c>&lt;inheritdoc cref="X"/&gt;</c> pointing at the documented member), so Roslyn cannot resolve it.
    /// </summary>
    CircularInheritDoc,

    /// <summary>
    /// An explicit <c>&lt;inheritdoc cref="X"/&gt;</c> names a target that the semantic model
    /// cannot resolve to a symbol.
    /// </summary>
    CrefTargetNotFound,

    /// <summary>
    /// The site could not be resolved for a reason that does not fit the other categories
    /// (for example the documented symbol could not be determined, or the resolved XML failed to parse).
    /// </summary>
    Other,
}
#pragma warning restore FDW017
