using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Logging;
using Fdw.Commands.Data.Upserts;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Bulk upsert command: the upsert for large loads. Same meaning as <see cref="UpsertCommand{T}"/>; it declares the
/// intent so a connection can use its bulk mechanism. Returns the number of rows written.
/// </summary>
/// <typeparam name="T">The type of the source rows (a dictionary of column values or an entity).</typeparam>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataCommands), "BulkUpsert", RestrictToCurrentCompilation = true)]
public sealed class BulkUpsertCommand<T> : DataCommandBase<int, IEnumerable<T>>, IUpsertCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkUpsertCommand{T}"/> class.
    /// </summary>
    /// <param name="data">The source rows.</param>
    /// <param name="options">The key, compare and write columns and optional behaviours.</param>
    public BulkUpsertCommand(IEnumerable<T> data, UpsertOptions options)
        : base("BulkUpsert", data)
    {
        Options = options;
        BulkUpsertCommandLog.CommandCreated(NullLogger<BulkUpsertCommand<T>>.Instance, typeof(T).Name);
    }

    /// <summary>Gets the key, compare and write columns and the optional versioning and insert-only behaviour.</summary>
    public UpsertOptions Options { get; }
}
