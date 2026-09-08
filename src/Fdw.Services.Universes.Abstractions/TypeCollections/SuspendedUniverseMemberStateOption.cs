using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>Still recorded, deliberately not in effect. Kept rather than deleted so restoring access does not mean reconstructing who they were.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseMemberStates), "Suspended")]
public sealed class SuspendedUniverseMemberStateOption : UniverseMemberStateBase
{
    /// <summary>Initializes a new instance of the <see cref="SuspendedUniverseMemberStateOption"/> class.</summary>
    public SuspendedUniverseMemberStateOption() : base("Suspended", grantsMembership: false)
    {
    }
}
