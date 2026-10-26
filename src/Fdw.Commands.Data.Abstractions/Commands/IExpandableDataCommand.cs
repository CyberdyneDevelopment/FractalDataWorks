using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// A data command a connection can run as simpler data commands when its kind has no translator for it. A bulk upsert
/// is the upsert of each row, a bulk update the update of each row, and an upsert on a kind with no upsert the delete
/// and insert of each row.
/// </summary>
/// <remarks>
/// The connection asks only when it has no translator for the command's type, so a kind with an optimized form of the
/// command is never expanded. The simpler commands are run in order through the same lookup, so one may expand again.
/// </remarks>
public interface IExpandableDataCommand : IDataCommand
{
    /// <summary>Makes the simpler commands that do what this command does, in the order they are to run.</summary>
    /// <param name="container">The container the commands are dispatched with, for the checks that need to see it.</param>
    /// <returns>The commands, each answering the rows it affected; or why this command cannot be expanded for the container.</returns>
    IGenericResult<IReadOnlyList<ExpandedCommand>> Expand(IStorageContainer container);
}
