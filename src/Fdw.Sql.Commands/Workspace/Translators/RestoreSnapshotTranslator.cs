using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions;
using Fdw.Sql.Commands.Abstractions.Results;
using Fdw.Sql.Commands.Logging;
using Fdw.Sql.Commands.Workspace.Commands;
using Fdw.Sql.Workspace;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Sql.Commands.Workspace.Translators;

[TypeOption(typeof(SqlWorkspaceTranslators), "RestoreSnapshot", RestrictToCurrentCompilation = true)]
public sealed class RestoreSnapshotTranslator : SqlCommandTranslatorBase<RestoreSnapshotCommand, MutationResult>
{
    public RestoreSnapshotTranslator() : base("RestoreSnapshot", "Restores a snapshot via the workspace.") { }

    public override Task<IGenericResult<MutationResult>> Translate(
        RestoreSnapshotCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<RestoreSnapshotTranslator>.Instance;
        RestoreSnapshotTranslatorLog.Translating(logger, command.SnapshotId ?? string.Empty);

        if (string.IsNullOrWhiteSpace(command.SnapshotId))
            return Task.FromResult<IGenericResult<MutationResult>>(
                GenericResult<MutationResult>.Failure(RestoreSnapshotTranslatorLog.SnapshotIdRequired(logger)));

        var restored = workspace.RestoreSnapshot(command.SnapshotId);
        if (!restored.IsSuccess)
            return Task.FromResult<IGenericResult<MutationResult>>(
                GenericResult<MutationResult>.Failure(
                    RestoreSnapshotTranslatorLog.RestoreFailed(logger, command.SnapshotId, restored.CurrentMessage ?? "Snapshot not found")));

        RestoreSnapshotTranslatorLog.SnapshotRestored(logger, command.SnapshotId);
        return Task.FromResult<IGenericResult<MutationResult>>(
            GenericResult<MutationResult>.Success(
                new MutationResult($"Restored snapshot '{command.SnapshotId}'", workspace.ScriptPaths)));
    }
}
