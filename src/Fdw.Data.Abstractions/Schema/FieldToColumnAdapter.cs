#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Schema;
using Fdw.Schema.Properties;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Adapter that converts an <see cref="IField"/> (logical field) to <see cref="IColumnDefinition"/> (physical column).
/// </summary>
/// <remarks>
/// This adapter bridges the logical-physical gap, mapping IField metadata to SQL column metadata.
/// Used when generating DDL or physical schemas from logical DataSet definitions.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class FieldToColumnAdapter : IColumnDefinition
{
    private readonly IField _field;

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldToColumnAdapter"/> class.
    /// </summary>
    /// <param name="field">The field to adapt to a column definition.</param>
    public FieldToColumnAdapter(IField field)
    {
        _field = field;
    }

    /// <inheritdoc/>
    public string Name => _field.Name;

    /// <inheritdoc/>
    public IPropertyRole Role => _field.Role;

    /// <inheritdoc/>
    public bool IsRequired => _field.IsRequired;

    /// <inheritdoc/>
    public string? Description => _field.Description;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object>? Metadata => _field.Metadata;

    /// <inheritdoc/>
    /// <remarks>
    /// Inferred from IField.FieldType. Default is NVarChar for flexibility.
    /// </remarks>
    public SqlDbType SqlType
    {
        get
        {
            // Map IFieldType to SqlDbType
            var typeSystemId = _field.TypeSystemId;
            if (string.Equals(typeSystemId, "MsSql", StringComparison.Ordinal) && _field.ConverterTypeId.HasValue)
            {
                // If we have MsSql type system info, use it
                return (SqlDbType)_field.ConverterTypeId.Value;
            }

            // Otherwise, infer from FieldType (simplified mapping)
            var fieldTypeName = _field.FieldType?.TypeName ?? string.Empty;
            return fieldTypeName.ToLowerInvariant() switch
            {
                "int" or "int32" => SqlDbType.Int,
                "long" or "int64" => SqlDbType.BigInt,
                "short" or "int16" => SqlDbType.SmallInt,
                "byte" => SqlDbType.TinyInt,
                "bool" or "boolean" => SqlDbType.Bit,
                "decimal" or "money" => SqlDbType.Decimal,
                "float" or "single" => SqlDbType.Real,
                "double" => SqlDbType.Float,
                "datetime" => SqlDbType.DateTime2,
                "datetimeoffset" => SqlDbType.DateTimeOffset,
                "date" => SqlDbType.Date,
                "time" => SqlDbType.Time,
                "guid" or "uniqueidentifier" => SqlDbType.UniqueIdentifier,
                "string" or "varchar" => SqlDbType.VarChar,
                "nvarchar" or "text" => SqlDbType.NVarChar,
                "binary" or "varbinary" => SqlDbType.VarBinary,
                _ => SqlDbType.NVarChar // Default to NVarChar for unknown types
            };
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Retrieved from IField.Metadata if available, otherwise null.
    /// </remarks>
    public int? MaxLength
    {
        get
        {
            if (_field.Metadata?.TryGetValue("MaxLength", out var value) == true && value is int maxLength)
            {
                return maxLength;
            }
            return null;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Retrieved from IField.Metadata if available, otherwise null.
    /// </remarks>
    public int? Precision
    {
        get
        {
            if (_field.Metadata?.TryGetValue("Precision", out var value) == true && value is int precision)
            {
                return precision;
            }
            return null;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Retrieved from IField.Metadata if available, otherwise null.
    /// </remarks>
    public int? Scale
    {
        get
        {
            if (_field.Metadata?.TryGetValue("Scale", out var value) == true && value is int scale)
            {
                return scale;
            }
            return null;
        }
    }

    /// <inheritdoc/>
    public bool IsIdentity => _field.IsIdentity;

    /// <inheritdoc/>
    /// <remarks>
    /// Retrieved from IField.Metadata if available, otherwise null.
    /// </remarks>
    public string? DefaultExpression
    {
        get
        {
            if (_field.Metadata?.TryGetValue("DefaultExpression", out var value) == true && value is string expr)
            {
                return expr;
            }
            return null;
        }
    }

    /// <inheritdoc/>
    public string? ComputedExpression => _field.IsComputed
        ? _field.Metadata?.TryGetValue("ComputedExpression", out var value) == true && value is string expr
            ? expr
            : null
        : null;

    /// <inheritdoc/>
    /// <remarks>
    /// Retrieved from IField.Metadata if available, otherwise null.
    /// </remarks>
    public string? Collation
    {
        get
        {
            if (_field.Metadata?.TryGetValue("Collation", out var value) == true && value is string collation)
            {
                return collation;
            }
            return null;
        }
    }
}
