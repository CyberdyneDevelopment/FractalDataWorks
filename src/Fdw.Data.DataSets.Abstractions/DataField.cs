using System;
using Fdw.Conventions;
using Fdw.Data.DataContainers.Abstractions;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Represents metadata for a field in a data set.
/// Defines the structure and characteristics of data that can be queried.
/// </summary>
public sealed class DataField : IDataField, IEquatable<DataField>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataField"/> class.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="type">The .NET type of the field.</param>
    /// <param name="isKey">Whether this field is part of the primary key.</param>
    /// <param name="isNullable">Whether this field can contain null values.</param>
    /// <param name="maxLength">The maximum length for string fields, if applicable.</param>
    /// <param name="description">Optional description of the field.</param>
    public DataField(
        string name,
        Type type,
        bool isKey = false,
        bool isNullable = false,
        int? maxLength = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Field name cannot be null or whitespace.", nameof(name));

        Name = name;
        Type = type ?? throw new ArgumentNullException(nameof(type));
        IsKey = isKey;
        IsNullable = isNullable;
        MaxLength = maxLength;
        Description = description;
    }

    /// <summary>
    /// Gets the name of the field.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the .NET type of the field.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Gets a value indicating whether this field is part of the primary key.
    /// </summary>
    public bool IsKey { get; }

    /// <summary>
    /// Gets a value indicating whether this field can contain null values.
    /// </summary>
    public bool IsNullable { get; }

    /// <summary>
    /// Gets the maximum length for string fields, if applicable.
    /// </summary>
    public int? MaxLength { get; }

    /// <summary>
    /// Gets the optional description of the field.
    /// </summary>
    public string? Description { get; }

    // IDataField explicit interface implementation
    Type IDataField.FieldType => Type;
    bool IDataField.IsRequired => !IsNullable;
    object? IDataField.DefaultValue => null;
    bool IDataField.IsCalculated => false;  // DataField represents source fields, not calculated
    Func<IDataRow, object>? IDataField.Calculator => null;

    /// <summary>
    /// Gets the display name for this field.
    /// </summary>
    public string DisplayName => Description ?? Name;

    /// <summary>
    /// Gets the SQL type name equivalent for this field type.
    /// </summary>
    public string SqlTypeName => GetSqlTypeName(Type, MaxLength);

    /// <summary>
    /// Gets a value indicating whether this field represents a numeric type.
    /// </summary>
    public bool IsNumeric => IsNumericType(Type);

    /// <summary>
    /// Gets a value indicating whether this field represents a date/time type.
    /// </summary>
    public bool IsDateTime => Type == typeof(DateTime) || Type == typeof(DateTime?) ||
                             Type == typeof(DateTimeOffset) || Type == typeof(DateTimeOffset?);

    /// <summary>
    /// Gets a value indicating whether this field represents a string type.
    /// </summary>
    public bool IsString => Type == typeof(string);

    /// <inheritdoc/>
    public bool Equals(DataField? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return string.Equals(Name, other.Name, StringComparison.Ordinal) &&
               Type.Equals(other.Type) &&
               IsKey == other.IsKey &&
               IsNullable == other.IsNullable &&
               MaxLength == other.MaxLength;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is DataField other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Type, IsKey, IsNullable, MaxLength);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var nullableIndicator = IsNullable ? "?" : "";
        var keyIndicator = IsKey ? " [Key]" : "";
        var lengthIndicator = MaxLength.HasValue ? $"({MaxLength})" : "";

        return $"{Name}: {Type.Name}{nullableIndicator}{lengthIndicator}{keyIndicator}";
    }

    /// <summary>
    /// Determines if the specified type is numeric.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is numeric; otherwise, false.</returns>
    [ConventionOverride(MaxCyclomaticComplexity = 15)]  // Numeric type detection for all primitive numeric types (signed, unsigned, floating-point)
    private static bool IsNumericType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType == typeof(byte) ||
               underlyingType == typeof(sbyte) ||
               underlyingType == typeof(short) ||
               underlyingType == typeof(ushort) ||
               underlyingType == typeof(int) ||
               underlyingType == typeof(uint) ||
               underlyingType == typeof(long) ||
               underlyingType == typeof(ulong) ||
               underlyingType == typeof(float) ||
               underlyingType == typeof(double) ||
               underlyingType == typeof(decimal);
    }

    /// <summary>
    /// Gets the SQL type name for a .NET type.
    /// </summary>
    /// <param name="type">The .NET type.</param>
    /// <param name="maxLength">The maximum length for string types.</param>
    /// <returns>The SQL type name.</returns>
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // CLR to SQL type mapping with comprehensive switch for primitives and common types
    private static string GetSqlTypeName(Type type, int? maxLength)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType.Name switch
        {
            nameof(String) => maxLength.HasValue ? $"NVARCHAR({maxLength})" : "NVARCHAR(MAX)",
            nameof(Int32) => "INT",
            nameof(Int64) => "BIGINT",
            nameof(Int16) => "SMALLINT",
            nameof(Byte) => "TINYINT",
            nameof(Boolean) => "BIT",
            nameof(DateTime) => "DATETIME2",
            nameof(DateTimeOffset) => "DATETIMEOFFSET",
            nameof(Decimal) => "DECIMAL(18,2)",
            nameof(Double) => "FLOAT",
            nameof(Single) => "REAL",
            nameof(Guid) => "UNIQUEIDENTIFIER",
            _ => "NVARCHAR(MAX)"
        };
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(DataField? left, DataField? right) =>
        left?.Equals(right) ?? right is null;

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(DataField? left, DataField? right) => !(left == right);
}