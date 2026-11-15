using System.ComponentModel.DataAnnotations.Schema;

namespace Fdw.Data.DataStores.SqlServer.Models;

/// <summary>
/// A row of INFORMATION_SCHEMA.TABLE_CONSTRAINTS (FDW-5876).
/// </summary>
[GenerateMapper]
public sealed class InformationSchemaTableConstraint
{
    /// <summary>Gets or sets the constraint name.</summary>
    [Column("CONSTRAINT_NAME")]
    public string ConstraintName { get; set; } = string.Empty;

    /// <summary>Gets or sets the table's schema.</summary>
    [Column("TABLE_SCHEMA")]
    public string TableSchema { get; set; } = string.Empty;

    /// <summary>Gets or sets the table name.</summary>
    [Column("TABLE_NAME")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>Gets or sets the constraint type (PRIMARY KEY, UNIQUE, FOREIGN KEY, CHECK).</summary>
    [Column("CONSTRAINT_TYPE")]
    public string ConstraintType { get; set; } = string.Empty;
}
