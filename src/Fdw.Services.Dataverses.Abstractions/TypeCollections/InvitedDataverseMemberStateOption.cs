using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>An invitation nobody has accepted. Not membership - the row records that someone was asked, which is a different fact from their being here.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseMemberStates), "Invited")]
public sealed class InvitedDataverseMemberStateOption : DataverseMemberStateBase
{
    /// <summary>Initializes a new instance of the <see cref="InvitedDataverseMemberStateOption"/> class.</summary>
    public InvitedDataverseMemberStateOption() : base("Invited", grantsMembership: false)
    {
    }
}
