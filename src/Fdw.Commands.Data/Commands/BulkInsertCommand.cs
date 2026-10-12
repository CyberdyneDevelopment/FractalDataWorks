using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Commands.Abstractions;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Bulk insert command for high-performance batch inserts using database-specific bulk mechanisms.
/// Returns the number of affected rows.
/// </summary>
/// <typeparam name="T">The type of entity to insert.</typeparam>
/// <remarks>
/// <para>
/// This command explicitly requests bulk insert operations that may use database-specific
/// optimizations like SqlBulkCopy, PostgreSQL COPY, MySQL LOAD DATA, etc.
/// </para>
/// <para>
/// A database connection runs it one of two ways (FDW-1152). Up to the connection's configured <c>MaxSerialInsert</c> rows it runs
/// serially, translated to batched multi-row INSERT statements like any insert; above that it takes the connection's bulk mode
/// (SQL Server: SqlBulkCopy, PostgreSQL: COPY), inside the unit of work when there is one. The command only answers which
/// (<see cref="ReroutesToBulk"/>); the connection acts on the answer.
/// </para>
/// <para>
/// ⚠️ IMPORTANT: Bulk operations may have different transaction/locking behavior.
/// Use this for large datasets (1000+ rows) where performance is critical.
/// For smaller batches or ACID-strict scenarios, use InsertCommand&lt;IEnumerable&lt;T&gt;&gt; instead.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var call = DataBulkInsert.Into&lt;TeamSeasonFact&gt;("NflData", "fct", "TeamSeason")
///     .Values(facts);
/// var result = await gateway.Execute&lt;int&gt;(call, ct);
/// </code>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataCommands), "BulkInsert", RestrictToCurrentCompilation = true)]
public sealed class BulkInsertCommand<T> : DataCommandBase<int, IEnumerable<T>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkInsertCommand{T}"/> class.
    /// </summary>
    /// <param name="data">The collection of entities to insert.</param>
    public BulkInsertCommand(IEnumerable<T> data)
        : base("BulkInsert", Rows(data))
    {
        BulkInsertCommandLog.CommandCreated(NullLogger<BulkInsertCommand<T>>.Instance, typeof(T).Name);
    }

    /// <inheritdoc/>
    /// <remarks>Yes when the command has more rows than the connection lets an insert run serially.</remarks>
    public override bool ReroutesToBulk(int maxSerialInsert) => Data is IReadOnlyCollection<T> rows && rows.Count > maxSerialInsert;

    // The rows are counted to choose the route, so a lazy sequence is read once, here, and the command holds what it will write.
    // The rows are non-null by type, as for every data command; a command whose input a translator cannot read is refused there with the coded MissingInputData failure.
    private static IEnumerable<T> Rows(IEnumerable<T> data) => data is IReadOnlyCollection<T> ? data : [.. data];
}
