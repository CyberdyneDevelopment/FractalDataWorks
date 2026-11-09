using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// Generate only output (write) mapping: GetPropertyNames and MapToParameters, for write-side connectors.
/// </summary>
[TypeOption(typeof(MapperDirections), "Output")]
[ExcludeFromCodeCoverage]
public sealed class OutputMapperDirection : MapperDirectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutputMapperDirection"/> class.
    /// </summary>
    public OutputMapperDirection()
        : base("Output", "Generate only output (write) mapping: GetPropertyNames and MapToParameters, for write-side connectors.", false, true)
    {
    }
}
