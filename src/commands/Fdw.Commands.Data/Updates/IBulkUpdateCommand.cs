using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data.Updates;

/// <summary>
/// A bulk update, read by a translator without knowing its row type: the key and write columns. The rows are the command's
/// data (FDW-4953).
/// </summary>
public interface IBulkUpdateCommand : IDataCommand
{
    /// <summary>Gets the key columns that select the row each source row updates, and the columns written.</summary>
    BulkUpdateOptions Options { get; }
}
