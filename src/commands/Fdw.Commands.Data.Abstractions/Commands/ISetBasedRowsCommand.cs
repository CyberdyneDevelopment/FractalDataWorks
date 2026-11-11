namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// A command that writes a whole batch of rows in one set-based statement (a bulk upsert, update or delete; FDW-4958). A connection reads how many
/// rows it carries to choose how to stage them: inline for a small batch, by its bulk mechanism for a large one.
/// </summary>
public interface ISetBasedRowsCommand : IDataCommand
{
    /// <summary>Gets how many source rows the command carries.</summary>
    int RowCount { get; }
}
