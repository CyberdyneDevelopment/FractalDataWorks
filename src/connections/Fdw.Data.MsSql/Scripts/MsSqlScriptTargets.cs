using System;
using System.Collections.Generic;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Data.MsSql;

/// <summary>
/// The first walk of a T-SQL parse tree (FDW-5457): the tables a script writes and creates, the names of its common table expressions, and the
/// table references that are targets, so the second walk does not count them as reads.
/// </summary>
/// <remarks>
/// A write is the target of an insert, update, delete, merge or <c>OUTPUT ... INTO</c>, a truncated table, and the table a <c>SELECT ... INTO</c>
/// makes. An update or delete that names its target by an alias from its FROM clause writes the table the alias stands for.
/// </remarks>
internal sealed class MsSqlScriptTargets : TSqlFragmentVisitor
{
    /// <summary>Gets the tables the script writes, as it names them.</summary>
    public List<SchemaObjectName> Writes { get; } = [];

    /// <summary>Gets the tables the script creates, as it names them.</summary>
    public List<SchemaObjectName> Creates { get; } = [];

    /// <summary>Gets the names of the script's common table expressions.</summary>
    public HashSet<string> CommonTableNames { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the table references that are written to, by reference.</summary>
    public HashSet<TSqlFragment> TargetReferences { get; } = new(ReferenceEqualityComparer.Instance);

    /// <inheritdoc />
    public override void Visit(InsertSpecification node) => Target(node.Target, null);

    /// <inheritdoc />
    public override void Visit(UpdateSpecification node) => Target(node.Target, node.FromClause);

    /// <inheritdoc />
    public override void Visit(DeleteSpecification node) => Target(node.Target, node.FromClause);

    /// <inheritdoc />
    public override void Visit(MergeSpecification node) => Target(node.Target, null);

    /// <inheritdoc />
    public override void Visit(OutputIntoClause node) => Target(node.IntoTable, null);

    /// <inheritdoc />
    public override void Visit(TruncateTableStatement node) => Writes.Add(node.TableName);

    /// <inheritdoc />
    public override void Visit(SelectStatement node)
    {
        if (node.Into is { } into)
        {
            Writes.Add(into);
            Creates.Add(into);
        }
    }

    /// <inheritdoc />
    public override void Visit(CreateTableStatement node) => Creates.Add(node.SchemaObjectName);

    /// <inheritdoc />
    public override void Visit(CommonTableExpression node) => CommonTableNames.Add(node.ExpressionName.Value);

    // A target that is not a named table (a table variable, a rowset function) is the script's own, and writes nothing in the store.
    private void Target(TableReference? target, FromClause? from)
    {
        if (target is not NamedTableReference named)
        {
            return;
        }

        TargetReferences.Add(named);
        if (MsSqlScriptAliases.Find(named, from) is { } aliased)
        {
            TargetReferences.Add(aliased);
            Writes.Add(aliased.SchemaObject);
            return;
        }

        Writes.Add(named.SchemaObject);
    }
}
