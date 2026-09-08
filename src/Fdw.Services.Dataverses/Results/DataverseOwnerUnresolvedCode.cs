using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Dataverses.Results;

/// <summary>
/// A dataverse was being created but the caller creating it could not be resolved to a user id.
/// </summary>
/// <remarks>
/// The create endpoint sits behind the <c>dataverses:write</c> policy, so an unauthenticated caller
/// cannot reach it. Arriving there with no authentication context, or with a UserId that is not a
/// Guid, is therefore an internal inconsistency rather than a rejected request — hence 500 rather
/// than a 401 or a validation failure.
///
/// Why this refuses instead of storing Guid.Empty: OwnerUserId is NOT NULL, so a zero Guid stores
/// cleanly and reads back as an owner that cannot be resolved. Ownership is also what
/// dataverses:write is to be scoped by, so a dataverse with no real owner is one nobody can edit
/// once that scoping lands — including whoever created it.
/// </remarks>
[TypeOption(typeof(DataversesResultCodes), "DataverseOwnerUnresolved", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataverseOwnerUnresolvedCode : DataversesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataverseOwnerUnresolvedCode"/> class.</summary>
    public DataverseOwnerUnresolvedCode()
        // 90001, not a second 90000: DataversesResultCodes declares ById unique, so two codes
        // sharing a number make the whole collection throw on first lookup -- which took every
        // dataverse write down, not just this code's own path. Ids increment within their
        // category band (see HttpResultCodes 71000-71005); the band carries the meaning and the
        // sequence keeps them distinct.
        : base(90001, "DataverseOwnerUnresolved",
            ResultSeverities.ByName("Error"),
            "Dataverse '{name}' cannot be created: the calling user could not be resolved ({reason})",
            isRetryable: false)
    {
    }
}
