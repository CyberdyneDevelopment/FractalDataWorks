using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Types;

/// <summary>
/// Service TypeCollection with factory and configuration support.
/// </summary>
[TypeOption(typeof(CollectionKinds), "Service")]
[ExcludeFromCodeCoverage]
public sealed class ServiceKind : CollectionKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceKind"/> class.
    /// </summary>
    public ServiceKind() : base(3, "Service")
    {
    }
}
