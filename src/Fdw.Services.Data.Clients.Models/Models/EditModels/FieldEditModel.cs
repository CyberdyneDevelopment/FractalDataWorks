using System;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Edit model for creating or updating a DataContainerField.
/// </summary>
public class FieldEditModel
{
    /// <summary>Gets or sets the client-side identity for this field.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the logical name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the data type (e.g. string, int, datetime).</summary>
    public string DataType { get; set; } = "string";

    /// <summary>Gets or sets whether the field is nullable.</summary>
    public bool IsNullable { get; set; }

    /// <summary>Gets or sets whether the field is part of the primary key.</summary>
    public bool IsKey { get; set; }

    /// <summary>Gets or sets the ordinal position of the field in the container.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the maximum length for string fields.</summary>
    public int? MaxLength { get; set; }

    /// <summary>Gets or sets the numeric precision.</summary>
    public int? Precision { get; set; }

    /// <summary>Gets or sets the numeric scale.</summary>
    public int? Scale { get; set; }

    /// <summary>Creates a shallow copy of this model.</summary>
    public FieldEditModel Clone()
    {
        return new FieldEditModel
        {
            Id = Id,
            Name = Name,
            DataType = DataType,
            IsNullable = IsNullable,
            IsKey = IsKey,
            Ordinal = Ordinal,
            MaxLength = MaxLength,
            Precision = Precision,
            Scale = Scale,
        };
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is not FieldEditModel other)
            return false;

        return Id == other.Id
            && string.Equals(Name, other.Name, StringComparison.Ordinal)
            && string.Equals(DataType, other.DataType, StringComparison.Ordinal)
            && IsNullable == other.IsNullable
            && IsKey == other.IsKey
            && Ordinal == other.Ordinal
            && MaxLength == other.MaxLength
            && Precision == other.Precision
            && Scale == other.Scale;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id);
        hash.Add(Name);
        hash.Add(DataType);
        hash.Add(IsNullable);
        hash.Add(IsKey);
        hash.Add(Ordinal);
        hash.Add(MaxLength);
        hash.Add(Precision);
        hash.Add(Scale);
        return hash.ToHashCode();
    }
}
