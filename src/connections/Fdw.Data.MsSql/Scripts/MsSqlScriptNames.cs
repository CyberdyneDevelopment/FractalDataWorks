using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Connections.Abstractions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Data.MsSql;

/// <summary>
/// Turns the names a T-SQL script gives into the catalogue's terms (FDW-5457): the schema is the path and the name is the container.
/// </summary>
/// <remarks>
/// A name the script holds for itself is dropped: a temporary table, and a one-part name of one of its common table expressions. A name with a
/// part given at run time is listed apart, and so is a name with a database or server part. Each name is listed once.
/// </remarks>
internal sealed class MsSqlScriptNames
{
    private const string TemporaryPrefix = "#";

    private readonly HashSet<string> _runTimeNames;
    private readonly HashSet<string> _commonTableNames;
    private readonly List<string> _outsideStore = [];
    private readonly List<string> _runTimeNamed = [];

    /// <summary>Initializes a new instance of the <see cref="MsSqlScriptNames"/> class.</summary>
    /// <param name="runTimeNames">The identifiers that stand for a name given at run time.</param>
    /// <param name="commonTableNames">The names of the script's common table expressions.</param>
    public MsSqlScriptNames(IReadOnlyCollection<string> runTimeNames, HashSet<string> commonTableNames)
    {
        _runTimeNames = new HashSet<string>(runTimeNames, StringComparer.OrdinalIgnoreCase);
        _commonTableNames = commonTableNames;
    }

    /// <summary>Gets the names of objects in another database or server, each once.</summary>
    public List<string> OutsideStore => _outsideStore;

    /// <summary>Gets the names with a part given at run time, each once.</summary>
    public List<string> RunTimeNamed => _runTimeNamed;

    /// <summary>Lists the names of the store's objects among those given, each once, and notes the others.</summary>
    /// <param name="names">The names, as the script gives them.</param>
    /// <returns>The store's objects, in the order the script first names them.</returns>
    public List<ScriptObjectName> Sort(IEnumerable<SchemaObjectName> names)
    {
        var found = new List<ScriptObjectName>();
        foreach (var name in names)
        {
            var objectName = InStore(name);
            if (objectName is not null && !found.Any(existing => Same(existing, objectName)))
            {
                found.Add(objectName);
            }
        }

        return found;
    }

    private ScriptObjectName? InStore(SchemaObjectName name)
    {
        var text = string.Join(".", name.Identifiers.Select(identifier => identifier.Value));
        if (name.Identifiers.Any(identifier => _runTimeNames.Contains(identifier.Value)))
        {
            Note(_runTimeNamed, text);
            return null;
        }

        if (name.BaseIdentifier.Value.StartsWith(TemporaryPrefix, StringComparison.Ordinal)
            || (name.SchemaIdentifier is null && _commonTableNames.Contains(name.BaseIdentifier.Value)))
        {
            return null;
        }

        if (name.DatabaseIdentifier is not null || name.ServerIdentifier is not null)
        {
            Note(_outsideStore, text);
            return null;
        }

        return new ScriptObjectName(name.SchemaIdentifier?.Value, name.BaseIdentifier.Value);
    }

    private static void Note(List<string> list, string text)
    {
        if (!list.Contains(text, StringComparer.OrdinalIgnoreCase))
        {
            list.Add(text);
        }
    }

    private static bool Same(ScriptObjectName left, ScriptObjectName right)
        => string.Equals(left.Path, right.Path, StringComparison.OrdinalIgnoreCase) && string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
}
