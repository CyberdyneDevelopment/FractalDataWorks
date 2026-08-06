using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Types;

/// <summary>
/// Standard immutable TypeCollection (compile-time fixed).
/// </summary>
[TypeOption(typeof(CollectionKinds), "Immutable")]
[ExcludeFromCodeCoverage]
public sealed class ImmutableKind : CollectionKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImmutableKind"/> class.
    /// </summary>
    public ImmutableKind() : base(0, "Immutable")
    {
    }
}
