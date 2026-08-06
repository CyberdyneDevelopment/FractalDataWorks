using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Commands.Abstractions;
using Fdw.Commands.Data.Abstractions;

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
/// Translators convert it to:
/// <list type="bullet">
/// <item>SQL Server: SqlBulkCopy (with constraints/triggers enabled)</item>
/// <item>PostgreSQL: COPY command</item>
/// <item>MySQL: LOAD DATA or batched multi-row INSERT</item>
/// <item>Other DBs: Batched multi-row INSERT (fallback)</item>
/// </list>
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
        : base("BulkInsert", data)
    {
    }
}
