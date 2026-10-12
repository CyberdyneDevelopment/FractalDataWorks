using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions;
using Fdw.Sql.Commands.Abstractions.Results;
using Fdw.Sql.Workspace;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Sql.Commands;

/// <summary>
/// Default <see cref="ISqlCommandHandler"/>. Looks up the translator, runs
/// it against the active workspace, and reflects on the command for
/// SnapshotId-style state (CreateSnapshot result patching, etc.).
/// </summary>
public sealed class SqlCommandHandler : ISqlCommandHandler
{
    private readonly ISqlWorkspace _workspace;
    private readonly ISqlTranslatorRegistry _translators;
    private readonly ILogger<SqlCommandHandler> _logger;

    public SqlCommandHandler(ISqlWorkspace workspace, ISqlTranslatorRegistry translators, ILogger<SqlCommandHandler>? logger = null)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _translators = translators ?? throw new ArgumentNullException(nameof(translators));
        _logger = logger ?? NullLogger<SqlCommandHandler>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<ISqlCommandResult>> Execute(ISqlCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return GenericResult<ISqlCommandResult>.Failure(SqlResultCodes.CommandCannotBeNull);

        var lookup = _translators.GetTranslator(command.GetType());
        if (!lookup.IsSuccess)
            return GenericResult<ISqlCommandResult>.Failure(SqlResultCodes.TranslatorNotFound,
                ResultDetails.Create("Message", lookup.CurrentMessage ?? "Unknown translator"));

        // CreateSnapshot: handler also calls _workspace.CreateSnapshot() and
        // patches the result's SnapshotId. Mirrors the Roslyn handler pattern.
        var commandName = command.Name;
        try
        {
            var result = await lookup.Value!.Execute(command, _workspace, cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess && string.Equals(commandName, "CreateSnapshot", StringComparison.Ordinal))
            {
                StorePersistedSnapshot(command, result.Value);
            }
            return result;
        }
        catch (OperationCanceledException ex)
        {
            return GenericResult<ISqlCommandResult>.Failure(SqlResultCodes.CommandExecutionCancelled,
                ResultDetails.Create("ErrorMessage", ex.Message));
        }
#pragma warning disable CA1031 // handler must not throw
        catch (Exception ex)
        {
            return GenericResult<ISqlCommandResult>.Failure(SqlResultCodes.CommandExecutionFailed,
                ResultDetails.Create("ErrorMessage", ex.Message));
        }
#pragma warning restore CA1031
    }

    private void StorePersistedSnapshot(ISqlCommand command, ISqlCommandResult? resultValue)
    {
        var cmdType = command.GetType();
        var nameProp = cmdType.GetProperty("SnapshotName", BindingFlags.Public | BindingFlags.Instance);
        var descProp = cmdType.GetProperty("SnapshotDescription", BindingFlags.Public | BindingFlags.Instance);
        var name = nameProp?.GetValue(command) as string;
        var desc = descProp?.GetValue(command) as string;
        if (string.IsNullOrWhiteSpace(name)) return;

        var realId = _workspace.CreateSnapshot(name, desc ?? string.Empty);

        if (resultValue is null) return;
        var dataProp = resultValue.GetType().GetProperty("Data", BindingFlags.Public | BindingFlags.Instance);
        var data = dataProp?.GetValue(resultValue);
        if (data is null) return;
        var idProp = data.GetType().GetProperty("SnapshotId", BindingFlags.Public | BindingFlags.Instance);
        if (idProp is null || !idProp.CanWrite) return;
        idProp.SetValue(data, realId);
    }
}
