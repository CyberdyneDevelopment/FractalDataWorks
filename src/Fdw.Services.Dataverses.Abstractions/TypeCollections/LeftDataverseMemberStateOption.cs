using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Gone. Kept for the history of who was here, and grants nothing.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseMemberStates), "Left")]
public sealed class LeftDataverseMemberStateOption : DataverseMemberStateBase
{
    /// <summary>Initializes a new instance of the <see cref="LeftDataverseMemberStateOption"/> class.</summary>
    public LeftDataverseMemberStateOption() : base("Left", grantsMembership: false)
    {
    }
}
