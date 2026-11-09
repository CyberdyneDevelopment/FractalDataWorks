using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// Generate only input (read) mapping: MapFromReader and MapFromDictionary. What a [GenerateMapper] with no Direction means.
/// </summary>
[TypeOption(typeof(MapperDirections), "Input")]
[ExcludeFromCodeCoverage]
public sealed class InputMapperDirection : MapperDirectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InputMapperDirection"/> class.
    /// </summary>
    public InputMapperDirection()
        : base("Input", "Generate only input (read) mapping: MapFromReader and MapFromDictionary. What a [GenerateMapper] with no Direction means.", true, false)
    {
    }
}
