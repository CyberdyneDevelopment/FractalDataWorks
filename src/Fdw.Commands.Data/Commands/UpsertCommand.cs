using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Logging;
using Fdw.Commands.Data.Upserts;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Upsert command: writes the rows into the target, inserting keys that do not exist and updating (or, with
/// versioning, closing and re-opening) keys whose compare columns changed. Returns the number of rows written.
/// </summary>
/// <typeparam name="T">The type of the source rows (a dictionary of column values or an entity).</typeparam>
/// <remarks>
/// <para>
/// The caller passes everything as configuration (<see cref="UpsertOptions"/>); nothing here names a table or
/// column. Translators turn it into one set-based statement per connection type. Rows that already match on every
/// compare column are left untouched and are not counted.
/// </para>
/// <para>
/// Use <see cref="BulkUpsertCommand{T}"/> for large loads; the two differ in intent and in how a connection may
/// optimise them, not in meaning.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataCommands), "Upsert", RestrictToCurrentCompilation = true)]
public sealed class UpsertCommand<T> : DataCommandBase<int, IEnumerable<T>>, IUpsertCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpsertCommand{T}"/> class.
    /// </summary>
    /// <param name="data">The source rows.</param>
    /// <param name="options">The key, compare and write columns and optional behaviours.</param>
    public UpsertCommand(IEnumerable<T> data, UpsertOptions options)
        : base("Upsert", data)
    {
        Options = options;
        UpsertCommandLog.CommandCreated(NullLogger<UpsertCommand<T>>.Instance, typeof(T).Name);
    }

    /// <summary>Gets the key, compare and write columns and the optional versioning and insert-only behaviour.</summary>
    public UpsertOptions Options { get; }
}
