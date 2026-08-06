using System.Collections.Generic;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Lightweight field mapping descriptor used by source mappers during record extraction.
/// Contains only the properties a mapper needs: logical name, physical name, and ordinal.
/// </summary>
/// <remarks>
/// Why: This type exists in Abstractions (netstandard2.0) so that <see cref="DataSetSourceMapperContext"/>
/// and <see cref="DataSetSourceMapperTypeBase"/> can reference field mappings without depending on the
/// full <c>DataSetFieldMappingConfiguration</c> class in the net10.0 DataSets project.
/// Callers convert from <c>DataSetFieldMappingConfiguration</c> to this type when building the context.
/// </remarks>
public sealed class SourceFieldMapping
{
    /// <summary>
    /// Gets or sets the logical field name from the DataSet schema (the target name).
    /// </summary>
    public string LogicalFieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the physical field name in the source (XPath expression, column name, etc.).
    /// </summary>
    public string PhysicalFieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ordinal position for ordering mappings.
    /// </summary>
    public int Ordinal { get; set; }

    /// <summary>
    /// Gets or sets the ordered transform chain to apply after the raw value is extracted.
    /// Applied in ascending Ordinal order. Empty list means pass-through.
    /// </summary>
    public IReadOnlyList<SourceFieldTransform> Transforms { get; set; } = [];
}
