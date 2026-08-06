using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Types;

/// <summary>
/// Service TypeCollection with pre-created instances.
/// </summary>
[TypeOption(typeof(CollectionKinds), "ServiceInstance")]
[ExcludeFromCodeCoverage]
public sealed class ServiceInstanceKind : CollectionKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceInstanceKind"/> class.
    /// </summary>
    public ServiceInstanceKind() : base(5, "ServiceInstance")
    {
    }
}
