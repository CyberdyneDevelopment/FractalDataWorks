using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// Represents a field mapping in a Map transform.
/// </summary>
public interface IFieldMapping
{
    /// <summary>
    /// Gets the source field name.
    /// </summary>
    string SourceField { get; }

    /// <summary>
    /// Gets the destination field name.
    /// </summary>
    string DestinationField { get; }

    /// <summary>
    /// Gets the optional transform expression.
    /// </summary>
    string? TransformExpression { get; }

    /// <summary>
    /// Gets the ordered chain of transforms to apply to this field, each with its configured
    /// parameter values. Applied in ascending <see cref="IFieldMappingTransform.Ordinal"/>.
    /// </summary>
    /// <remarks>
    /// Why a chain of steps with parameters rather than the single <see cref="TransformExpression"/>
    /// string above: the stored configuration has always been a chain - transform.FieldMappingTransform
    /// rows, each owning transform.FieldMappingTransformParameter rows - and the dataset source mappers
    /// have always read it that way. This runtime shape could carry only a bare name, so a Map transform
    /// could name a transform but never configure it, and every transform requiring a parameter ran
    /// with none. Both readers now express the same thing the configuration stores.
    /// </remarks>
    IReadOnlyList<IFieldMappingTransform> Transforms { get; }

    /// <summary>
    /// Gets the default value if source is null.
    /// </summary>
    string? DefaultValue { get; }

    /// <summary>
    /// Gets the target type name for type conversion.
    /// </summary>
    /// <remarks>
    /// Supported types: string, int, long, decimal, double, bool, datetime, guid
    /// </remarks>
    string? TargetType { get; }

    /// <summary>
    /// Gets whether this field is required.
    /// </summary>
    bool IsRequired { get; }

    /// <summary>
    /// Gets whether this mapping is enabled.
    /// </summary>
    bool IsEnabled { get; }
}
