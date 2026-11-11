using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Data.MsSql;

/// <summary>
/// A SQL Server translator whose set-based write can also be staged by bulk copy (FDW-4958): the connection asks it for a
/// <see cref="MsSqlStagedWrite"/> when a batch is too large to send inline.
/// </summary>
public interface IMsSqlStagingTranslator
{
    /// <summary>Plans the command's write for staging by bulk copy, or says why it cannot be written.</summary>
    /// <param name="command">The command.</param>
    /// <param name="container">The target.</param>
    /// <returns>The staged write, or the coded failure the inline translation would give.</returns>
    IGenericResult<MsSqlStagedWrite> Stage(IDataCommand command, IStorageContainer container);
}
