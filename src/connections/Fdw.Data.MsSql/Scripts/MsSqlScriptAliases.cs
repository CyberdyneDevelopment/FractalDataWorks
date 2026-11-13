using System;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Data.MsSql;

/// <summary>Finds the table an alias stands for in a FROM clause (FDW-5457), for an update or delete that names its target by the alias.</summary>
internal sealed class MsSqlScriptAliases : TSqlFragmentVisitor
{
    private readonly string _alias;

    private MsSqlScriptAliases(string alias)
    {
        _alias = alias;
    }

    /// <summary>Gets the table the alias stands for; null until it is found.</summary>
    public NamedTableReference? Found { get; private set; }

    /// <summary>Finds the table a one-part target names by alias in the FROM clause.</summary>
    /// <param name="target">The target of the update or delete.</param>
    /// <param name="from">Its FROM clause; null when it has none.</param>
    /// <returns>The table the alias stands for; null when the target is not an alias of the clause.</returns>
    public static NamedTableReference? Find(NamedTableReference target, FromClause? from)
    {
        if (from is null || target.SchemaObject.SchemaIdentifier is not null)
        {
            return null;
        }

        var finder = new MsSqlScriptAliases(target.SchemaObject.BaseIdentifier.Value);
        from.Accept(finder);
        return finder.Found;
    }

    /// <inheritdoc />
    public override void Visit(NamedTableReference node)
    {
        if (Found is null && node.Alias is { } alias && string.Equals(alias.Value, _alias, StringComparison.OrdinalIgnoreCase))
        {
            Found = node;
        }
    }
}
