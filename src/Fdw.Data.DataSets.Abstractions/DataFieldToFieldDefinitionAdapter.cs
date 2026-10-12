#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Schema;
using Fdw.Schema.Properties;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Adapter that converts a <see cref="DataSetFieldConfiguration"/> to <see cref="IFieldDefinition"/>.
/// </summary>
/// <remarks>
/// This adapter bridges configuration objects (used for IOptions binding) to schema interfaces
/// (used for DDL generation and data operations).
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class DataFieldToFieldDefinitionAdapter : IFieldDefinition
{
    private readonly DataSetFieldConfiguration _config;
    private readonly IDatumType _role;
    private readonly Type _clrType;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataFieldToFieldDefinitionAdapter"/> class.
    /// </summary>
    /// <param name="config">The field configuration to adapt.</param>
    /// <param name="roleProvider">Optional function to resolve role by name. If null, defaults to AttributeRole.</param>
    public DataFieldToFieldDefinitionAdapter(
        DataSetFieldConfiguration config,
        Func<string?, IDatumType>? roleProvider = null)
    {
        _config = config;

        // Resolve role
        _role = roleProvider?.Invoke(config.Role) ?? DatumTypes.Attribute;

        // The field's data type is a DataTypes option name; one that is not has no CLR type to adapt to.
        _clrType = DataSetFieldTypes.Resolve(config.TypeName)
            ?? throw new ArgumentException(
                $"Field '{config.Name}' states data type '{config.TypeName}', which is not a DataTypes option.", nameof(config));
    }

    /// <inheritdoc/>
    public string Name => _config.Name;

    /// <inheritdoc/>
    public IDatumType Role => _role;

    /// <inheritdoc/>
    public bool IsRequired => _config.IsRequired;

    /// <inheritdoc/>
    public string? Description => _config.Description;

    /// <inheritdoc/>
    /// <remarks>
    /// Constructs metadata dictionary from configuration properties.
    /// </remarks>
    public IReadOnlyDictionary<string, object>? Metadata
    {
        get
        {
            var metadata = new Dictionary<string, object>(StringComparer.Ordinal);

            if (_config.MaxLength.HasValue)
            {
                metadata["MaxLength"] = _config.MaxLength.Value;
            }

            if (!string.IsNullOrWhiteSpace(_config.DefaultValue))
            {
                metadata["DefaultValue"] = _config.DefaultValue!;
            }

            if (_config.IsIndexed)
            {
                metadata["IsIndexed"] = true;
            }

            if (_config.IsKey)
            {
                metadata["IsKey"] = true;
            }

            return metadata.Count > 0 ? metadata : null;
        }
    }

    /// <inheritdoc/>
    public Type ClrType => _clrType;

    /// <inheritdoc/>
    /// <remarks>
    /// DataSetFieldConfiguration doesn't have explicit source mapping - returns null.
    /// </remarks>
    public string? SourceMapping => null;

    /// <inheritdoc/>
    /// <remarks>
    /// Returns the calculator function name if the field is calculated.
    /// </remarks>
    public string? Calculator => _config.IsCalculated ? "CustomCalculator" : null;

    /// <inheritdoc/>
    /// <remarks>
    /// DataSetFieldConfiguration doesn't have transformer info - returns null.
    /// </remarks>
    public string? Transformer => null;

    /// <inheritdoc/>
    /// <remarks>
    /// DataSetFieldConfiguration doesn't have format info - returns null.
    /// </remarks>
    public string? Format => null;
}
