using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>Here, and holding whatever their role describes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseMemberStates), "Active")]
public sealed class ActiveUniverseMemberStateOption : UniverseMemberStateBase
{
    /// <summary>Initializes a new instance of the <see cref="ActiveUniverseMemberStateOption"/> class.</summary>
    public ActiveUniverseMemberStateOption() : base("Active", grantsMembership: true)
    {
    }
}
