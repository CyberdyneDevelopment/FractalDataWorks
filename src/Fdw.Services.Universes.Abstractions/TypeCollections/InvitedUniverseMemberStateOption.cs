using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>An invitation nobody has accepted. Not membership - the row records that someone was asked, which is a different fact from their being here.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseMemberStates), "Invited")]
public sealed class InvitedUniverseMemberStateOption : UniverseMemberStateBase
{
    /// <summary>Initializes a new instance of the <see cref="InvitedUniverseMemberStateOption"/> class.</summary>
    public InvitedUniverseMemberStateOption() : base("Invited", grantsMembership: false)
    {
    }
}
