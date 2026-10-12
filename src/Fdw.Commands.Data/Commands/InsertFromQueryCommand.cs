using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Loads;
using Fdw.Commands.Data.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Insert-from-query command: a set-based INSERT ... SELECT on one connection. The source may be in another
/// database on the same server. Returns the number of rows inserted.
/// </summary>
/// <remarks>
/// Rows never leave the database, so this is the load to use when the source and the destination share a
/// connection. The write columns, the source and the value sources are configuration (<see cref="InsertFromQueryOptions"/>); a
/// value source can supply a column the source does not have, including the next value of a sequence in the order of
/// the listed columns.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataCommands), "InsertFromQuery", RestrictToCurrentCompilation = true)]
public sealed class InsertFromQueryCommand : DataCommandBase<int>, IInsertFromQueryCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InsertFromQueryCommand"/> class.
    /// </summary>
    /// <param name="options">The write columns, the value sources, and the source rows.</param>
    public InsertFromQueryCommand(InsertFromQueryOptions options)
        : base("InsertFromQuery")
    {
        Options = options;
        InsertFromQueryCommandLog.CommandCreated(NullLogger<InsertFromQueryCommand>.Instance, options.TargetColumns.Count);
    }

    /// <summary>Gets the write columns, the value sources, and the source rows.</summary>
    public InsertFromQueryOptions Options { get; }
}
