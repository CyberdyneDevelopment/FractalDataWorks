using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Universes.Results;

/// <summary>
/// The caller holds <c>universes:write</c> but is neither the owner of this universe nor a member
/// of it with a role that may change it.
/// </summary>
/// <remarks>
/// Category 5 (Auth), package band 51000 — the caller is authenticated and carries the permission,
/// so this is a refusal about THIS resource rather than about the request or the identity.
/// <para>
/// The permission grants the ability to have universes at all; ownership and membership decide
/// which ones. Before this existed, <c>universes:write</c> meant every universe in the tenant:
/// <c>security.UniversePolicy</c> filters on tenant and visibility group and says nothing about
/// ownership, the endpoints never read <c>OwnerUserId</c> or <c>UniverseMember</c>, and the four
/// member roles were a vocabulary no authorization path consulted.
/// </para>
/// </remarks>
[TypeOption(typeof(UniversesResultCodes), "UniverseWriteNotPermitted", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UniverseWriteNotPermittedCode : UniversesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="UniverseWriteNotPermittedCode"/> class.</summary>
    public UniverseWriteNotPermittedCode()
        : base(51000, "UniverseWriteNotPermitted",
            ResultSeverities.ByName("Error"),
            "Universe '{name}' cannot be changed by this caller: {reason}",
            isRetryable: false)
    {
    }
}
