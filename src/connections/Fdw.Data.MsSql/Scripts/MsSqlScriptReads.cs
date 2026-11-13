using System.Collections.Generic;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Data.MsSql;

/// <summary>The second walk of a T-SQL parse tree (FDW-5457): every named table reference that is not a target is a read.</summary>
internal sealed class MsSqlScriptReads : TSqlFragmentVisitor
{
    private readonly HashSet<TSqlFragment> _targets;

    /// <summary>Initializes a new instance of the <see cref="MsSqlScriptReads"/> class.</summary>
    /// <param name="targets">The table references the first walk found to be written to.</param>
    public MsSqlScriptReads(HashSet<TSqlFragment> targets)
    {
        _targets = targets;
    }

    /// <summary>Gets the tables the script reads, as it names them.</summary>
    public List<SchemaObjectName> Reads { get; } = [];

    /// <inheritdoc />
    public override void Visit(NamedTableReference node)
    {
        if (!_targets.Contains(node))
        {
            Reads.Add(node.SchemaObject);
        }
    }
}
