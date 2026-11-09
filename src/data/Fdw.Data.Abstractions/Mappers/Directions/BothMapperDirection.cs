using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// Generate both input and output mapping, for types that participate in both reads and writes.
/// </summary>
[TypeOption(typeof(MapperDirections), "Both")]
[ExcludeFromCodeCoverage]
public sealed class BothMapperDirection : MapperDirectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BothMapperDirection"/> class.
    /// </summary>
    public BothMapperDirection()
        : base("Both", "Generate both input and output mapping, for types that participate in both reads and writes.", true, true)
    {
    }
}
