using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Commands.Data.Ddl;

/// <summary>
/// Fluent command for altering database tables (ALTER TABLE).
/// </summary>
/// <remarks>
/// <para>
/// This command provides a backend-agnostic way to modify existing table schemas.
/// Supports adding, dropping, and modifying columns.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var command = new AlterTableCommand("EmailConfigurations")
/// {
///     SchemaName = "config"
/// }
/// .AddColumn("MaxAttachmentSize", SqlDbType.Int, isRequired: true, defaultValue: "10485760")
/// .AddColumn("AllowExternalRecipients", SqlDbType.Bit, isRequired: true, defaultValue: "1")
/// .ModifyColumn("SmtpHost", SqlDbType.NVarChar, maxLength: 500)
/// .DropColumn("OldColumnName");
///
/// var result = await dataGateway.Execute(command);
/// </code>
/// </para>
/// </remarks>
[SuppressMessage("Fdw.Collections.Analyzers", "TC001", Justification = "DDL commands are not part of a TypeCollection")]
public sealed class AlterTableCommand : DdlCommand
{
    private readonly List<AlterTableOperation> _operations = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AlterTableCommand"/> class.
    /// </summary>
    /// <param name="tableName">The name of the table to alter.</param>
    public AlterTableCommand(string tableName)
        : base(DdlCommandTypes.AlterTable, tableName)
    {
    }

    /// <summary>
    /// Gets the operations to perform on the table.
    /// </summary>
    /// <value>Read-only list of alter operations.</value>
    public IReadOnlyList<AlterTableOperation> Operations => _operations;

    /// <summary>
    /// Adds a new column to the table.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <param name="type">The SQL data type.</param>
    /// <param name="maxLength">The maximum length for string/binary types (use -1 for MAX).</param>
    /// <param name="precision">The precision for numeric types (total digits).</param>
    /// <param name="scale">The scale for numeric types (digits after decimal).</param>
    /// <param name="isRequired">True if the column is NOT NULL; otherwise, false.</param>
    /// <param name="defaultValue">The default value SQL expression (e.g., "GETUTCDATE()", "0").</param>
    /// <param name="isUnique">True if the column has a unique constraint; otherwise, false.</param>
    /// <param name="collation">The collation name for string columns.</param>
    /// <returns>This command instance for fluent chaining.</returns>
    public AlterTableCommand AddColumn(
        string name,
        SqlDbType type,
        int? maxLength = null,
        int? precision = null,
        int? scale = null,
        bool isRequired = false,
        string? defaultValue = null,
        bool isUnique = false,
        string? collation = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Column name cannot be null or whitespace.", nameof(name));

        var column = new ColumnDefinition
        {
            Name = name,
            Type = type,
            MaxLength = maxLength,
            Precision = precision,
            Scale = scale,
            IsRequired = isRequired,
            DefaultValue = defaultValue,
            IsUnique = isUnique,
            Collation = collation
        };

        _operations.Add(new AlterTableOperation
        {
            OperationType = AlterTableOperationTypes.AddColumn,
            ColumnDefinition = column
        });

        return this;
    }

    /// <summary>
    /// Drops a column from the table.
    /// </summary>
    /// <param name="name">The column name to drop.</param>
    /// <returns>This command instance for fluent chaining.</returns>
    public AlterTableCommand DropColumn(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Column name cannot be null or whitespace.", nameof(name));

        _operations.Add(new AlterTableOperation
        {
            OperationType = AlterTableOperationTypes.DropColumn,
            ColumnName = name
        });

        return this;
    }

    /// <summary>
    /// Modifies an existing column's definition.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <param name="newType">The new SQL data type.</param>
    /// <param name="maxLength">The maximum length for string/binary types (use -1 for MAX).</param>
    /// <param name="precision">The precision for numeric types (total digits).</param>
    /// <param name="scale">The scale for numeric types (digits after decimal).</param>
    /// <param name="isRequired">True if the column is NOT NULL; otherwise, false.</param>
    /// <param name="defaultValue">The default value SQL expression (e.g., "GETUTCDATE()", "0").</param>
    /// <param name="collation">The collation name for string columns.</param>
    /// <returns>This command instance for fluent chaining.</returns>
    public AlterTableCommand ModifyColumn(
        string name,
        SqlDbType newType,
        int? maxLength = null,
        int? precision = null,
        int? scale = null,
        bool isRequired = false,
        string? defaultValue = null,
        string? collation = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Column name cannot be null or whitespace.", nameof(name));

        var column = new ColumnDefinition
        {
            Name = name,
            Type = newType,
            MaxLength = maxLength,
            Precision = precision,
            Scale = scale,
            IsRequired = isRequired,
            DefaultValue = defaultValue,
            Collation = collation
        };

        _operations.Add(new AlterTableOperation
        {
            OperationType = AlterTableOperationTypes.ModifyColumn,
            ColumnDefinition = column
        });

        return this;
    }

    /// <summary>
    /// Renames a column.
    /// </summary>
    /// <param name="oldName">The current column name.</param>
    /// <param name="newName">The new column name.</param>
    /// <returns>This command instance for fluent chaining.</returns>
    public AlterTableCommand RenameColumn(string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(oldName))
            throw new ArgumentException("Old column name cannot be null or whitespace.", nameof(oldName));

        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("New column name cannot be null or whitespace.", nameof(newName));

        _operations.Add(new AlterTableOperation
        {
            OperationType = AlterTableOperationTypes.RenameColumn,
            ColumnName = oldName,
            NewColumnName = newName
        });

        return this;
    }

    /// <summary>
    /// Adds a foreign key constraint to the table.
    /// </summary>
    /// <param name="columnName">The column name in the current table.</param>
    /// <param name="referencedTable">The referenced (parent) table name.</param>
    /// <param name="referencedColumn">The referenced column name in the parent table.</param>
    /// <param name="onDelete">The action to take when the referenced row is deleted (default: NoAction).</param>
    /// <param name="onUpdate">The action to take when the referenced row is updated (default: NoAction).</param>
    /// <param name="referencedSchema">The schema of the referenced table.</param>
    /// <param name="constraintName">The name of the foreign key constraint (null to auto-generate).</param>
    /// <returns>This command instance for fluent chaining.</returns>
    public AlterTableCommand AddForeignKey(
        string columnName,
        string referencedTable,
        string referencedColumn,
        IForeignKeyAction? onDelete = null,
        IForeignKeyAction? onUpdate = null,
        string? referencedSchema = null,
        string? constraintName = null)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name cannot be null or whitespace.", nameof(columnName));

        if (string.IsNullOrWhiteSpace(referencedTable))
            throw new ArgumentException("Referenced table name cannot be null or whitespace.", nameof(referencedTable));

        if (string.IsNullOrWhiteSpace(referencedColumn))
            throw new ArgumentException("Referenced column name cannot be null or whitespace.", nameof(referencedColumn));

        var foreignKey = new ForeignKeyDefinition
        {
            Name = constraintName,
            ColumnName = columnName,
            ReferencedTable = referencedTable,
            ReferencedColumn = referencedColumn,
            OnDelete = onDelete ?? ForeignKeyActions.NoAction,
            OnUpdate = onUpdate ?? ForeignKeyActions.NoAction,
            ReferencedSchema = referencedSchema
        };

        _operations.Add(new AlterTableOperation
        {
            OperationType = AlterTableOperationTypes.AddForeignKey,
            ForeignKeyDefinition = foreignKey
        });

        return this;
    }

    /// <summary>
    /// Drops a constraint from the table.
    /// </summary>
    /// <param name="constraintName">The name of the constraint to drop.</param>
    /// <returns>This command instance for fluent chaining.</returns>
    public AlterTableCommand DropConstraint(string constraintName)
    {
        if (string.IsNullOrWhiteSpace(constraintName))
            throw new ArgumentException("Constraint name cannot be null or whitespace.", nameof(constraintName));

        _operations.Add(new AlterTableOperation
        {
            OperationType = AlterTableOperationTypes.DropConstraint,
            ConstraintName = constraintName
        });

        return this;
    }
}