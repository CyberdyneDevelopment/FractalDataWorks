using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Procedures;

/// <summary>
/// The stored procedures this endpoint runs, found by name with or without the sys schema.
/// </summary>
/// <remarks>A name no option answers to is error 2812. The set is open: a package adds a procedure by declaring an option.</remarks>
[TypeCollection(typeof(SqlProcedureBase), typeof(ISqlProcedure), typeof(SqlProcedures))]
[ExcludeFromCodeCoverage]
public abstract partial class SqlProcedures : TypeCollectionBase<SqlProcedureBase, ISqlProcedure>
{
    private static readonly Lazy<Dictionary<string, ISqlProcedure>> BySqlName =
        new(static () => All().ToDictionary(p => p.SqlName, p => (ISqlProcedure)p, StringComparer.OrdinalIgnoreCase));

    /// <summary>Finds a procedure by name.</summary>
    /// <param name="name">The procedure's name, as called.</param>
    /// <param name="procedure">The procedure, when this endpoint runs one by that name.</param>
    /// <returns>True when found.</returns>
    public static bool TryFind(string name, [NotNullWhen(true)] out ISqlProcedure? procedure)
    {
        ArgumentNullException.ThrowIfNull(name);
        var unqualified = name.StartsWith("sys.", StringComparison.OrdinalIgnoreCase) ? name[4..] : name;
        return BySqlName.Value.TryGetValue(unqualified, out procedure);
    }
}
