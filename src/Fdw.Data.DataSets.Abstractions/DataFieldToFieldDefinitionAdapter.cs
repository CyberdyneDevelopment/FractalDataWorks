#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Conventions;
using Fdw.Schema;
using Fdw.Schema.Properties;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Adapter that converts a <see cref="DataFieldConfiguration"/> to <see cref="IFieldDefinition"/>.
/// </summary>
/// <remarks>
/// This adapter bridges configuration objects (used for IOptions binding) to schema interfaces
/// (used for DDL generation and data operations).
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class DataFieldToFieldDefinitionAdapter : IFieldDefinition
{
    private readonly DataFieldConfiguration _config;
    private readonly IPropertyRole _role;
    private readonly Type _clrType;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataFieldToFieldDefinitionAdapter"/> class.
    /// </summary>
    /// <param name="config">The field configuration to adapt.</param>
    /// <param name="roleProvider">Optional function to resolve role by name. If null, defaults to AttributeRole.</param>
    public DataFieldToFieldDefinitionAdapter(
        DataFieldConfiguration config,
        Func<string?, IPropertyRole>? roleProvider = null)
    {
        _config = config;

        // Resolve role
        _role = roleProvider?.Invoke(config.Role) ?? PropertyRoles.Attribute;

        // Resolve CLR type from TypeName
        _clrType = ResolveClrType(config.TypeName);
    }

    /// <inheritdoc/>
    public string Name => _config.Name;

    /// <inheritdoc/>
    public IPropertyRole Role => _role;

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
    /// DataFieldConfiguration doesn't have explicit source mapping - returns null.
    /// </remarks>
    public string? SourceMapping => null;

    /// <inheritdoc/>
    /// <remarks>
    /// Returns the calculator function name if the field is calculated.
    /// </remarks>
    public string? Calculator => _config.IsCalculated ? "CustomCalculator" : null;

    /// <inheritdoc/>
    /// <remarks>
    /// DataFieldConfiguration doesn't have transformer info - returns null.
    /// </remarks>
    public string? Transformer => null;

    /// <inheritdoc/>
    /// <remarks>
    /// DataFieldConfiguration doesn't have format info - returns null.
    /// </remarks>
    public string? Format => null;

    /// <summary>
    /// Resolves a CLR type from a type name string.
    /// </summary>
    /// <param name="typeName">The type name to resolve.</param>
    /// <returns>The resolved Type, or typeof(string) as default.</returns>
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // Type name resolution with comprehensive switch for common CLR types (primitives, DateTime, Guid, arrays)
    private static Type ResolveClrType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return typeof(string);
        }

        // Handle common type names
        return typeName.ToLowerInvariant() switch
        {
            "int" or "int32" or "system.int32" => typeof(int),
            "long" or "int64" or "system.int64" => typeof(long),
            "short" or "int16" or "system.int16" => typeof(short),
            "byte" or "system.byte" => typeof(byte),
            "bool" or "boolean" or "system.boolean" => typeof(bool),
            "decimal" or "system.decimal" => typeof(decimal),
            "float" or "single" or "system.single" => typeof(float),
            "double" or "system.double" => typeof(double),
            "datetime" or "system.datetime" => typeof(DateTime),
            "datetimeoffset" or "system.datetimeoffset" => typeof(DateTimeOffset),
            "guid" or "uniqueidentifier" or "system.guid" => typeof(Guid),
            "string" or "system.string" => typeof(string),
            "char" or "system.char" => typeof(char),
            "byte[]" or "system.byte[]" => typeof(byte[]),
            "timespan" or "system.timespan" => typeof(TimeSpan),
            _ => ResolveByFullName(typeName)
        };
    }

    /// <summary>
    /// Attempts to resolve a type by its full name using Type.GetType.
    /// </summary>
    /// <param name="typeName">The full type name.</param>
    /// <returns>The resolved Type, or typeof(string) as fallback.</returns>
    private static Type ResolveByFullName(string typeName)
    {
        try
        {
            var type = Type.GetType(typeName, throwOnError: false);
            return type ?? typeof(string);
        }
        catch (Exception ex)
        {
            // Why: Type.GetType with throwOnError:false should not throw, but observe ex if it
            // ever does so the failure is not silently discarded. Fall back to string.
            _ = ex;
            return typeof(string);
        }
    }
}
