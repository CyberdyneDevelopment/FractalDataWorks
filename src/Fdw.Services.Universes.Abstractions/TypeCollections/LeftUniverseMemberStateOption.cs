using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>Gone. Kept for the history of who was here, and grants nothing.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseMemberStates), "Left")]
public sealed class LeftUniverseMemberStateOption : UniverseMemberStateBase
{
    /// <summary>Initializes a new instance of the <see cref="LeftUniverseMemberStateOption"/> class.</summary>
    public LeftUniverseMemberStateOption() : base("Left", grantsMembership: false)
    {
    }
}
