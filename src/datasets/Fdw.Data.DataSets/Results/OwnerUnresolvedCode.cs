using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A data set was being created but the caller creating it could not be resolved to a user id (FDW-4346).
/// </summary>
/// <remarks>
/// The create endpoint sits behind a policy, so an unauthenticated caller cannot reach it. Arriving there with no authentication context, or with a UserId that is
/// not a Guid, is an internal inconsistency rather than a rejected request, hence an internal-category code (as <c>WorkspaceOwnerUnresolved</c> is). The endpoint
/// refuses instead of storing no owner: a data set the list shows as Unowned must be one made before owners existed, not one made by a caller nobody could name.
/// </remarks>
[TypeOption(typeof(DataSetsResultCodes), "OwnerUnresolved", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OwnerUnresolvedCode : DataSetsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="OwnerUnresolvedCode"/> class.</summary>
    public OwnerUnresolvedCode()
        : base(90001, "OwnerUnresolved",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' cannot be created: the calling user could not be resolved ({reason})",
            isRetryable: false)
    {
    }
}
