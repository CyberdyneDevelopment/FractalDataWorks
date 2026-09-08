using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Dataverses.Results;

/// <summary>
/// The caller holds <c>dataverses:write</c> but is neither the owner of this dataverse nor a member
/// of it with a role that may change it.
/// </summary>
/// <remarks>
/// Category 5 (Auth), package band 51000 — the caller is authenticated and carries the permission,
/// so this is a refusal about THIS resource rather than about the request or the identity.
/// <para>
/// The permission grants the ability to have dataverses at all; ownership and membership decide
/// which ones. Before this existed, <c>dataverses:write</c> meant every dataverse in the tenant:
/// <c>security.DataversePolicy</c> filters on tenant and visibility group and says nothing about
/// ownership, the endpoints never read <c>OwnerUserId</c> or <c>DataverseMember</c>, and the four
/// member roles were a vocabulary no authorization path consulted.
/// </para>
/// </remarks>
[TypeOption(typeof(DataversesResultCodes), "DataverseWriteNotPermitted", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataverseWriteNotPermittedCode : DataversesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataverseWriteNotPermittedCode"/> class.</summary>
    public DataverseWriteNotPermittedCode()
        : base(51000, "DataverseWriteNotPermitted",
            ResultSeverities.ByName("Error"),
            "Dataverse '{name}' cannot be changed by this caller: {reason}",
            isRetryable: false)
    {
    }
}
