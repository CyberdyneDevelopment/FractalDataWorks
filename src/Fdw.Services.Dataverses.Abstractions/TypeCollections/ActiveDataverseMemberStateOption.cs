using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Here, and holding whatever their role describes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseMemberStates), "Active")]
public sealed class ActiveDataverseMemberStateOption : DataverseMemberStateBase
{
    /// <summary>Initializes a new instance of the <see cref="ActiveDataverseMemberStateOption"/> class.</summary>
    public ActiveDataverseMemberStateOption() : base("Active", grantsMembership: true)
    {
    }
}
