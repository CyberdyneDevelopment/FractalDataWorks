using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Still recorded, deliberately not in effect. Kept rather than deleted so restoring access does not mean reconstructing who they were.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseMemberStates), "Suspended")]
public sealed class SuspendedDataverseMemberStateOption : DataverseMemberStateBase
{
    /// <summary>Initializes a new instance of the <see cref="SuspendedDataverseMemberStateOption"/> class.</summary>
    public SuspendedDataverseMemberStateOption() : base("Suspended", grantsMembership: false)
    {
    }
}
