using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Appears in search and listings, so someone can ask to join. Its contents stay closed.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseVisibilities), "Discoverable")]
public sealed class DiscoverableDataverseVisibilityOption : DataverseVisibilityBase
{
    /// <summary>Initializes a new instance of the <see cref="DiscoverableDataverseVisibilityOption"/> class.</summary>
    public DiscoverableDataverseVisibilityOption() : base("Discoverable")
    {
    }
}
