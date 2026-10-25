using System.Collections.Generic;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// A data command that carries many rows and can be cut into commands of at most a given number of rows. A connection whose kind
/// sends only so many rows in one request (an HTTP protocol that takes one row, or a fixed-size list) runs the pieces one after the
/// other instead of refusing the command.
/// </summary>
public interface IBatchableDataCommand : IDataCommand
{
    /// <summary>Cuts this command into commands of at most the given number of rows, in the order of the rows.</summary>
    /// <param name="rows">The most rows one command holds; at least 1.</param>
    /// <returns>The commands; one holding every row when the rows fit, and none when this command carries no row.</returns>
    IReadOnlyList<IDataCommand> InBatchesOf(int rows);
}
