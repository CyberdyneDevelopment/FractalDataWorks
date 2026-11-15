using System.ComponentModel.DataAnnotations.Schema;

namespace Fdw.Data.DataStores.SqlServer.Models;

/// <summary>
/// A row of INFORMATION_SCHEMA.PARAMETERS (FDW-5876).
/// </summary>
[GenerateMapper]
public sealed class InformationSchemaParameter
{
    /// <summary>Gets or sets the owning routine's schema.</summary>
    [Column("SPECIFIC_SCHEMA")]
    public string SpecificSchema { get; set; } = string.Empty;

    /// <summary>Gets or sets the owning routine's name.</summary>
    [Column("SPECIFIC_NAME")]
    public string SpecificName { get; set; } = string.Empty;

    /// <summary>Gets or sets the parameter's position.</summary>
    [Column("ORDINAL_POSITION")]
    public int OrdinalPosition { get; set; }

    /// <summary>Gets or sets IN, OUT or INOUT; absent for a return value.</summary>
    [Column("PARAMETER_MODE")]
    public string? ParameterMode { get; set; }

    /// <summary>Gets or sets the parameter name.</summary>
    [Column("PARAMETER_NAME")]
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>Gets or sets the data type name.</summary>
    [Column("DATA_TYPE")]
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets the maximum character length.</summary>
    [Column("CHARACTER_MAXIMUM_LENGTH")]
    public int? CharacterMaximumLength { get; set; }

    /// <summary>Gets or sets the numeric precision.</summary>
    [Column("NUMERIC_PRECISION")]
    public byte? NumericPrecision { get; set; }

    /// <summary>Gets or sets the numeric scale.</summary>
    [Column("NUMERIC_SCALE")]
    public int? NumericScale { get; set; }
}
