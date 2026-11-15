namespace Fdw.Data.DataStores.SqlServer.Catalog;

/// <summary>A column or a procedure parameter the catalog describes (FDW-5876).</summary>
public sealed class CatalogColumn
{
    /// <summary>Gets or sets the column or parameter name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the SQL Server data type name.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the column accepts null.</summary>
    public bool IsNullable { get; set; }

    /// <summary>Gets or sets the maximum character length.</summary>
    public int? MaxLength { get; set; }

    /// <summary>Gets or sets the numeric precision.</summary>
    public byte? Precision { get; set; }

    /// <summary>Gets or sets the numeric scale.</summary>
    public int? Scale { get; set; }

    /// <summary>Gets or sets the column default.</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Gets or sets whether the column is part of the primary key.</summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>Gets or sets whether the column is an identity column.</summary>
    public bool IsIdentity { get; set; }

    /// <summary>Gets or sets whether the column is computed.</summary>
    public bool IsComputed { get; set; }
}
