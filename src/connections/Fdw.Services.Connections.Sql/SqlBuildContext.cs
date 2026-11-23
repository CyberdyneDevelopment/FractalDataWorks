using System;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Sql;

/// <summary>
/// State threaded through the recursive building of a WHERE clause: the dialect, where a parameter is added, the parameter prefix, the number of
/// parameters written so far and the table that qualifies a bare column. It is public only so the public SQL filter node handlers
/// (<see cref="FilterNodes.SqlFilterNodeHandlers"/>) can name it; it has no public member and is made by the translator.
/// </summary>
public sealed class SqlBuildContext
{
    internal SqlBuildContext(ISqlDialect dialect, Action<string, object?> addParam, string parameterPrefix, string? primaryQualifier)
    {
        Dialect = dialect;
        AddParam = addParam;
        ParameterPrefix = parameterPrefix;
        PrimaryQualifier = primaryQualifier;
    }

    internal ISqlDialect Dialect { get; }

    internal Action<string, object?> AddParam { get; }

    internal string ParameterPrefix { get; }

    internal int ParameterCounter { get; set; }

    internal string? PrimaryQualifier { get; }
}
