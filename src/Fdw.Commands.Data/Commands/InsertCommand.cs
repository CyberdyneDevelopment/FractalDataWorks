using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Commands.Abstractions;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Insert command for adding new records (INSERT operation), one row or many.
/// Returns the number of affected rows.
/// </summary>
/// <typeparam name="T">The type of row to insert.</typeparam>
/// <remarks>
/// <para>
/// This command represents a universal INSERT operation that works across all data sources.
/// Translators convert it to:
/// <list type="bullet">
/// <item>SQL: INSERT INTO statements</item>
/// <item>REST: POST requests, one per row unless the protocol sends a list in one request</item>
/// <item>File: Append records</item>
/// <item>GraphQL: createX mutations</item>
/// </list>
/// </para>
/// <para>
/// A database connection runs it one of two ways (FDW-1152). Up to the connection's configured <c>MaxSerialInsert</c> rows it runs
/// serially, translated to batched multi-row INSERT statements; above that it takes the connection's bulk mode when it has one
/// (SQL Server: SqlBulkCopy, PostgreSQL: COPY), inside the unit of work when there is one. The command only answers which
/// (<see cref="ReroutesToBulk"/>); the connection acts on the answer.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var customer = new Customer { Name = "Acme Corp", IsActive = true };
/// var call = DataInsert.Into&lt;Customer&gt;("CRM", "sales", "Customers").Value(customer);
/// var result = await gateway.Execute&lt;int&gt;(call, ct);
/// </code>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataCommands), "Insert", RestrictToCurrentCompilation = true)]
public sealed class InsertCommand<T> : DataCommandBase<int, IEnumerable<T>>, IReturningCommand, IBatchableDataCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InsertCommand{T}"/> class for one row.
    /// </summary>
    /// <param name="row">The row to insert.</param>
    public InsertCommand(T row)
        : base("Insert", [row])
    {
        InsertCommandLog.CommandCreated(NullLogger<InsertCommand<T>>.Instance, typeof(T).Name);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InsertCommand{T}"/> class for many rows.
    /// </summary>
    /// <param name="rows">The rows to insert.</param>
    public InsertCommand(IEnumerable<T> rows)
        : base("Insert", Rows(rows))
    {
        InsertCommandLog.CommandCreated(NullLogger<InsertCommand<T>>.Instance, typeof(T).Name);
    }

    /// <inheritdoc />
    public IReadOnlyList<string>? ReturnColumns { get; init; }

    /// <inheritdoc/>
    public IReadOnlyList<IDataCommand> InBatchesOf(int rows)
    {
        var held = Data.ToList();
        var batches = new List<IDataCommand>();
        for (var start = 0; start < held.Count; start += rows)
        {
            batches.Add(new InsertCommand<T>(held.GetRange(start, Math.Min(rows, held.Count - start))) { ReturnColumns = ReturnColumns, Metadata = Metadata });
        }

        return batches;
    }

    /// <inheritdoc/>
    public override bool ReroutesToBulk(int maxSerialInsert) => Data is IReadOnlyCollection<T> rows && rows.Count > maxSerialInsert;

    // The rows are counted to choose the route, so a lazy sequence is read once, here, and the command holds what it will write.
    // The rows are non-null by type, as for every data command; a command whose input a translator cannot read is refused there with the coded MissingInputData failure.
    private static IEnumerable<T> Rows(IEnumerable<T> data) => data is IReadOnlyCollection<T> ? data : [.. data];
}
