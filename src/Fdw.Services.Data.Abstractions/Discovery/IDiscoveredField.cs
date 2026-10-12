namespace Fdw.Services.Data.Abstractions.Discovery;

/// <summary>
/// A field/column on an <see cref="IDiscoveredContainer"/>.
/// </summary>
public interface IDiscoveredField
{
    /// <summary>The field's name.</summary>
    string Name { get; }

    /// <summary>The field's source data type (e.g. <c>nvarchar</c>, <c>int</c>).</summary>
    string DataType { get; }

    /// <summary>True if the source allows null for this field.</summary>
    bool IsNullable { get; }

    /// <summary>One-based ordinal position within the container.</summary>
    int Ordinal { get; }

    /// <summary>Maximum length for variable-width string/binary types, if known.</summary>
    int? MaxLength { get; }

    /// <summary>Numeric precision, if known.</summary>
    int? Precision { get; }

    /// <summary>Numeric scale, if known.</summary>
    int? Scale { get; }
}
