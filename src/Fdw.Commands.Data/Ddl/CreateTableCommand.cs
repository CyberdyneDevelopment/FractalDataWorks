using System;
using System.Collections.Generic;
using System.Data;

namespace Fdw.Commands.Data.Ddl;

/// <summary>
/// Fluent command for creating database tables (CREATE TABLE).
/// </summary>
/// <remarks>
/// <para>
/// This command provides a backend-agnostic way to define table schemas.
/// Translators convert it to the appropriate DDL syntax for each database:
/// <list type="bullet">
/// <item>SQL Server: CREATE TABLE [schema].[table] (...)</item>
/// <item>PostgreSQL: CREATE TABLE schema.table (...)</item>
/// <item>MySQL: CREATE TABLE schema.table (...)</item>
/// <item>SQLite: CREATE TABLE table (...)</item>
/// </list>
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var command = new CreateTableCommand("EmailConfigurations")
/// {
///     SchemaName = "config",
///     IfNotExists = true
/// }
/// .WithColumn("Id", SqlDbType.Int, isPrimaryKey: true, isIdentity: true, isRequired: true)
/// .WithColumn("SmtpHost", SqlDbType.NVarChar, maxLength: 255, isRequired: true)
/// .WithColumn("SmtpPort", SqlDbType.Int, isRequired: true, defaultValue: "587")
/// .WithColumn("CreatedAt", SqlDbType.DateTime2, isRequired: true, defaultValue: "GETUTCDATE()")
/// .WithForeignKey("ConnectionTypeId", "ConnectionTypes", "Id", onDelete: new CascadeForeignKeyAction())
/// .WithIndex("IX_EmailConfigurations_CreatedAt", new[] { "CreatedAt" });
///
/// var result = await dataGateway.Execute(command);
/// </code>
/// </para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Fdw.Collections.Analyzers", "TC001", Justification = "DDL commands are not part of a TypeCollection")]
public sealed class CreateTableCommand : DdlCommand
{
    private readonly List<ColumnDefinition> _columns = new();
    private readonly List<ForeignKeyDefinition> _foreignKeys = new();
    private readonly List<IndexDefinition> _indexes = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateTableCommand"/> class.
    /// </summary>
    /// <param name="tableName">The name of the table to create.</param>
    public CreateTableCommand(string tableName)
        : base(DdlCommandTypes.CreateTable, tableName)
    {
    }

    /// <summary>
    /// Gets the columns to be created in the table.
    /// </summary>
    /// <value>Read-only list of column definitions.</value>
    public IReadOnlyList<ColumnDefinition> Columns => _columns;

    /// <summary>
    /// Gets the foreign key constraints for the table.
    /// </summary>
    /// <value>Read-only list of foreign key definitions.</value>
    public IReadOnlyList<ForeignKeyDefinition> ForeignKeys => _foreignKeys;

    /// <summary>
    /// Gets the indexes to be created on the table.
    /// </summary>
    /// <value>Read-only list of index definitions.</value>
    public IReadOnlyList<IndexDefinition> Indexes => _indexes;

    /// <summary>
    /// Adds a column to the table definition.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <param name="type">The SQL data type.</param>
    /// <param name="maxLength">The maximum length for string/binary types (use -1 for MAX).</param>
    /// <param name="precision">The precision for numeric types (total digits).</param>
    /// <param name="scale">The scale for numeric types (digits after decimal).</param>
    /// <param name="isRequired">True if the column is NOT NULL; otherwise, false.</param>
    /// <param name="isPrimaryKey">True if the column is part of the primary key; otherwise, false.</param>
    /// <param name="isIdentity">True if the column is an identity/auto-increment column; otherwise, false.</param>
    /// <param name="defaultValue">The default value SQL expression (e.g., "GETUTCDATE()", "0").</param>
    /// <param name="isUnique">True if the column has a unique constraint; otherwise, false.</param>
    /// <param name="collation">The collation name for string columns.</param>
    /// <returns>This command instance for fluent chaining.</returns>
    public CreateTableCommand WithColumn(
        string name,
        SqlDbType type,
        int? maxLength = null,
        int? precision = null,
        int? scale = null,
        bool isRequired = false,
        bool isPrimaryKey = false,
        bool isIdentity = false,
        string? defaultValue = null,
        bool isUnique = false,
        string? collation = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Column name cannot be null or whitespace.", nameof(name));

        _columns.Add(new ColumnDefinition
        {
            Name = name,
            Type = type,
            MaxLength = maxLength,
            Precision = precision,
            Scale = scale,
            IsRequired = isRequired,
            IsIdentity = isIdentity,
            DefaultValue = defaultValue,
            IsUnique = isUnique,
            Collation = collation
        });

        return this;
    }

    /// <summary>
    /// Adds a computed column to the table definition.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <param name="type">The SQL data type.</param>
    /// <param name="computedExpression">The SQL expression for computing the column value.</param>
    /// <param name="isRequired">True if the column is NOT NULL; otherwise, false.</param>
    /// <returns>This command instance for fluent chaining.</returns>
    public CreateTableCommand WithComputedColumn(
        string name,
        SqlDbType type,
        string computedExpression,
        bool isRequired = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Column name cannot be null or whitespace.", nameof(name));

        if (string.IsNullOrWhiteSpace(computedExpression))
            throw new ArgumentException("Computed expression cannot be null or whitespace.", nameof(computedExpression));

        _columns.Add(new ColumnDefinition
        {
            Name = name,
            Type = type,
            IsComputed = true,
            ComputedExpression = computedExpression,
            IsRequired = isRequired
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
    public CreateTableCommand WithForeignKey(
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

        _foreignKeys.Add(new ForeignKeyDefinition
        {
            Name = constraintName,
            ColumnName = columnName,
            ReferencedTable = referencedTable,
            ReferencedColumn = referencedColumn,
            OnDelete = onDelete ?? ForeignKeyActions.NoAction,
            OnUpdate = onUpdate ?? ForeignKeyActions.NoAction,
            ReferencedSchema = referencedSchema
        });

        return this;
    }

    /// <summary>
    /// Adds an index to the table.
    /// </summary>
    /// <param name="indexName">The name of the index.</param>
    /// <param name="columnNames">The column names to include in the index (order matters).</param>
    /// <param name="isUnique">True if the index enforces uniqueness; otherwise, false.</param>
    /// <param name="isClustered">True if the index is clustered; otherwise, false.</param>
    /// <param name="includeColumns">The columns to include in the index leaf pages (covering index).</param>
    /// <param name="filterCondition">The WHERE clause for a filtered index.</param>
    /// <param name="fillFactor">The fill factor percentage (1-100).</param>
    /// <returns>This command instance for fluent chaining.</returns>
    public CreateTableCommand WithIndex(
        string indexName,
        string[] columnNames,
        bool isUnique = false,
        bool isClustered = false,
        string[]? includeColumns = null,
        string? filterCondition = null,
        int? fillFactor = null)
    {
        if (string.IsNullOrWhiteSpace(indexName))
            throw new ArgumentException("Index name cannot be null or whitespace.", nameof(indexName));

        if (columnNames == null || columnNames.Length == 0)
            throw new ArgumentException("At least one column name must be specified.", nameof(columnNames));

        _indexes.Add(new IndexDefinition
        {
            Name = indexName,
            ColumnNames = columnNames,
            IsUnique = isUnique,
            IsClustered = isClustered,
            IncludeColumns = includeColumns ?? Array.Empty<string>(),
            FilterCondition = filterCondition,
            FillFactor = fillFactor
        });

        return this;
    }
}
