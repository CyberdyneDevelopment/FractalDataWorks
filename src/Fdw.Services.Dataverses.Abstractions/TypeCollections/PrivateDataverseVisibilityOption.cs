using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Invisible to non-members. Only a member can find it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseVisibilities), "Private")]
public sealed class PrivateDataverseVisibilityOption : DataverseVisibilityBase
{
    /// <summary>Initializes a new instance of the <see cref="PrivateDataverseVisibilityOption"/> class.</summary>
    public PrivateDataverseVisibilityOption() : base("Private")
    {
    }
}
