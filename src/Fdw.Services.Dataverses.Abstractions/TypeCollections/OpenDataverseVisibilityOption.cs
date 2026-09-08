using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Appears to everyone in the tenant, and its contents are readable without joining.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseVisibilities), "Open")]
public sealed class OpenDataverseVisibilityOption : DataverseVisibilityBase
{
    /// <summary>Initializes a new instance of the <see cref="OpenDataverseVisibilityOption"/> class.</summary>
    public OpenDataverseVisibilityOption() : base("Open")
    {
    }
}
