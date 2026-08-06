using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Types;

/// <summary>
/// Mutable TypeCollection (runtime registration supported).
/// </summary>
[TypeOption(typeof(CollectionKinds), "Mutable")]
[ExcludeFromCodeCoverage]
public sealed class MutableKind : CollectionKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MutableKind"/> class.
    /// </summary>
    public MutableKind() : base(1, "Mutable")
    {
    }
}
