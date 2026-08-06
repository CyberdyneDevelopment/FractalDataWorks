using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Types;

/// <summary>
/// Mutable Service TypeCollection with runtime registration.
/// </summary>
[TypeOption(typeof(CollectionKinds), "MutableService")]
[ExcludeFromCodeCoverage]
public sealed class MutableServiceKind : CollectionKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MutableServiceKind"/> class.
    /// </summary>
    public MutableServiceKind() : base(4, "MutableService")
    {
    }
}
