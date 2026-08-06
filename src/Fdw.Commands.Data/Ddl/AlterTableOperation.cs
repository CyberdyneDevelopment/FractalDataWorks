namespace Fdw.Commands.Data.Ddl;

/// <summary>
/// Represents a single operation in an ALTER TABLE command.
/// </summary>
// Why: pure data holder, no logic beyond trivial construction/assignment
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class AlterTableOperation
{
    /// <summary>
    /// Gets or sets the type of alter operation.
    /// </summary>
    public required IAlterTableOperationType OperationType { get; init; }

    /// <summary>
    /// Gets or sets the column name (for drop/rename operations).
    /// </summary>
    public string? ColumnName { get; init; }

    /// <summary>
    /// Gets or sets the new column name (for rename operations).
    /// </summary>
    public string? NewColumnName { get; init; }

    /// <summary>
    /// Gets or sets the column definition (for add/modify operations).
    /// </summary>
    public ColumnDefinition? ColumnDefinition { get; init; }

    /// <summary>
    /// Gets or sets the foreign key definition (for add foreign key operations).
    /// </summary>
    public ForeignKeyDefinition? ForeignKeyDefinition { get; init; }

    /// <summary>
    /// Gets or sets the constraint name (for drop constraint operations).
    /// </summary>
    public string? ConstraintName { get; init; }
}