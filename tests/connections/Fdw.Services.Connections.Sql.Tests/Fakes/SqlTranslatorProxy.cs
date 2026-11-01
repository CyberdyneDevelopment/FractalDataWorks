using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Sql.Tests.Fakes;

/// <summary>
/// Thin subclass of SqlDataCommandTranslatorBase that promotes the protected static helpers
/// to internal static so the test suite can drive them directly with a fake dialect and path,
/// independent of any real SQL backend.
/// </summary>
/// <remarks>
/// Why: the shared static helpers (BuildQualifiedTableName, BuildWhereClause,
/// BuildOrderByClause, IsValidColumnName) are all protected — they can only be called from
/// a subclass. This proxy is NOT a TypeOption; it has no [TypeOption] attribute and is never
/// registered in a TypeCollection. It only exists to bridge the access-modifier gap in tests.
/// TCommand = object because there is no useful ADO.NET command to produce; Translate() is
/// a stub that returns a dummy success. Tests call the Expose* wrappers directly.
/// </remarks>
internal sealed class SqlTranslatorProxy : SqlDataCommandTranslatorBase<object>
{
    public SqlTranslatorProxy() : base("TestProxy", "Fake") { }

    public override Task<IGenericResult<object>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
        => Task.FromResult(GenericResult<object>.Success(new object()));

    // ── Exposed protected statics ───────────────────────────────────────────

    public static string ExposeQualifiedTableName(IDatabasePath path)
        => BuildQualifiedTableName(path);

    public static string ExposeSchemaQualifiedTableName(IDatabasePath path)
        => BuildSchemaQualifiedTableName(path);

    public static string ExposeWhereClause(
        IFilterExpression filter,
        ISqlDialect dialect,
        Action<string, object?> addParam,
        string? primaryTableQualifier = null,
        string parameterPrefix = "@")
        => BuildWhereClause(filter, dialect, addParam, primaryTableQualifier, parameterPrefix);

    public static string ExposeOrderByClause(IOrderingExpression ordering, ISqlDialect dialect)
        => BuildOrderByClause(ordering, dialect);

    public static bool ExposeIsValidColumnName(string name)
        => IsValidColumnName(name);

    public static string ExposeSoftDeleteUpdate(
        string table,
        ISqlDialect dialect,
        SoftDeleteOptions options,
        IStorageContainer container,
        string now,
        string? filterPredicate,
        Action<string, object?> addParameter)
        => BuildSoftDeleteUpdate(table, dialect, options, container, now, filterPredicate, addParameter);

    public static void ExposeRefuseSoftDeleteOnUpdate(IDataCommand command, IStorageContainer container)
        => RefuseSoftDeleteOnUpdate(command, container);

    public static IReadOnlyList<string> ExposeReturnedColumns(IDataCommand command, IStorageContainer container)
        => ReturnedColumnsOf(command, container);
}
