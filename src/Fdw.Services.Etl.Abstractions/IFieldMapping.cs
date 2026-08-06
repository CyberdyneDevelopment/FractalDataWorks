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
