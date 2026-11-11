using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Sql;

/// <summary>What every SQL kind's bulk update and delete read of a command that reports its unmatched keys (FDW-4988).</summary>
public static class SetBasedUnmatchedKeys
{
    /// <summary>Reads how many unmatched keys a command asks for: none when it does not report them, a refusal when it asks for fewer than none.</summary>
    /// <param name="command">The command.</param>
    /// <returns>The sample size, <see langword="null"/> for a command that does not report, or the refusal.</returns>
    public static IGenericResult<int?> SampleOf(IDataCommand command)
    {
        if (command is not IReportsUnmatchedKeys { UnmatchedKeySample: { } sample })
            return GenericResult<int?>.Success(null);
        return sample < 0
            ? GenericResult<int?>.Failure(CommandOptionsLog.UnmatchedKeySampleNegative(NullLogger.Instance, command.CommandType, sample))
            : GenericResult<int?>.Success(sample);
    }
}
