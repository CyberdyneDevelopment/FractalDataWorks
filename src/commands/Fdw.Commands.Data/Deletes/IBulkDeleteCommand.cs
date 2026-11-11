using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data.Deletes;

/// <summary>
/// A bulk delete, read by a translator without knowing its row type: the key columns. The rows, which carry the keys, are the
/// command's data (FDW-4954).
/// </summary>
public interface IBulkDeleteCommand : IDataCommand
{
    /// <summary>Gets the columns whose values select the row each source row deletes.</summary>
    BulkDeleteOptions Options { get; }
}
