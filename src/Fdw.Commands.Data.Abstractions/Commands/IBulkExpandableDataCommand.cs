using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// An expandable data command that replaces rows differently above the number of rows a connection runs serially: an upsert
/// of more rows than that deletes the matching rows by key and inserts every row at once, instead of replacing each row on
/// its own.
/// </summary>
/// <remarks>
/// A connection passes its <c>MaxSerialInsert</c> when it has one. A connection with no such setting expands the command
/// through <see cref="IExpandableDataCommand.Expand(IStorageContainer)"/>.
/// </remarks>
public interface IBulkExpandableDataCommand : IExpandableDataCommand
{
    /// <summary>Makes the simpler commands for a connection that runs up to <paramref name="maxSerialInsert"/> rows serially.</summary>
    /// <param name="container">The container the commands are dispatched with, for the checks that need to see it.</param>
    /// <param name="maxSerialInsert">The most rows the connection runs serially before it takes its bulk route.</param>
    /// <returns>The commands, each answering the rows it affected; or why this command cannot be expanded for the container.</returns>
    IGenericResult<IReadOnlyList<ExpandedCommand>> Expand(IStorageContainer container, int maxSerialInsert);
}
