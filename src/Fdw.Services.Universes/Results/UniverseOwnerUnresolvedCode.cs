using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Universes.Results;

/// <summary>
/// A universe was being created but the caller creating it could not be resolved to a user id.
/// </summary>
/// <remarks>
/// The create endpoint sits behind the <c>universes:write</c> policy, so an unauthenticated caller
/// cannot reach it. Arriving there with no authentication context, or with a UserId that is not a
/// Guid, is therefore an internal inconsistency rather than a rejected request — hence 500 rather
/// than a 401 or a validation failure.
///
/// Why this refuses instead of storing Guid.Empty: OwnerUserId is NOT NULL, so a zero Guid stores
/// cleanly and reads back as an owner that cannot be resolved. Ownership is also what
/// universes:write is to be scoped by, so a universe with no real owner is one nobody can edit
/// once that scoping lands — including whoever created it.
/// </remarks>
[TypeOption(typeof(UniversesResultCodes), "UniverseOwnerUnresolved", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UniverseOwnerUnresolvedCode : UniversesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="UniverseOwnerUnresolvedCode"/> class.</summary>
    public UniverseOwnerUnresolvedCode()
        : base(90000, "UniverseOwnerUnresolved",
            ResultSeverities.ByName("Error"),
            "Universe '{name}' cannot be created: the calling user could not be resolved ({reason})",
            isRetryable: false)
    {
    }
}
