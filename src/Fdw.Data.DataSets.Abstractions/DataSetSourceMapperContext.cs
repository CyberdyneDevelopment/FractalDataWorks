using System.Collections.Generic;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Context bag passed to source mappers during record extraction.
/// Contains the raw payload, record selector expression, and resolved field mappings.
/// </summary>
public sealed class DataSetSourceMapperContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetSourceMapperContext"/> class.
    /// </summary>
    /// <param name="payload">The raw payload from the connection/acquisition layer.</param>
    /// <param name="contentType">The content type hint for payload format validation.</param>
    /// <param name="recordSelector">The expression identifying repeating record elements.</param>
    /// <param name="fieldMappings">The resolved field mappings for this source.</param>
    public DataSetSourceMapperContext(
        object payload,
        string contentType,
        string recordSelector,
        IReadOnlyList<SourceFieldMapping> fieldMappings)
    {
        Payload = payload;
        ContentType = contentType;
        RecordSelector = recordSelector;
        FieldMappings = fieldMappings;
    }

    /// <summary>
    /// Gets the raw payload returned by the connection/acquisition layer.
    /// Each mapper casts to its expected type (e.g., XElement, string) and returns Failure on mismatch.
    /// </summary>
    public object Payload { get; }

    /// <summary>
    /// Gets the content type hint so the mapper can validate it received the expected format.
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// Gets the record selector expression that identifies repeating record elements in the payload.
    /// For XPath: "//Report/Data/Row". For JSONPath: "$.data[*]".
    /// </summary>
    public string RecordSelector { get; }

    /// <summary>
    /// Gets the resolved field mappings for this source, ordered by Ordinal.
    /// Each mapping's PhysicalFieldName is evaluated relative to the current record element.
    /// </summary>
    public IReadOnlyList<SourceFieldMapping> FieldMappings { get; }
}
