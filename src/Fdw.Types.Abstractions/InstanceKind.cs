using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Types;

/// <summary>
/// TypeCollection with pre-created instances instead of types.
/// </summary>
[TypeOption(typeof(CollectionKinds), "Instance")]
[ExcludeFromCodeCoverage]
public sealed class InstanceKind : CollectionKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InstanceKind"/> class.
    /// </summary>
    public InstanceKind() : base(2, "Instance")
    {
    }
}
