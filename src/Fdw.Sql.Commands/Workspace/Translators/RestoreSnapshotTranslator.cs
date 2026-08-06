using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions;
using Fdw.Sql.Commands.Abstractions.Results;
using Fdw.Sql.Commands.Workspace.Commands;
using Fdw.Sql.Workspace;

namespace Fdw.Sql.Commands.Workspace.Translators;

[TypeOption(typeof(SqlWorkspaceTranslators), "RestoreSnapshot", RestrictToCurrentCompilation = true)]
public sealed class RestoreSnapshotTranslator : SqlCommandTranslatorBase<RestoreSnapshotCommand, MutationResult>
{
    public RestoreSnapshotTranslator() : base("RestoreSnapshot", "Restores a snapshot via the workspace.") { }

    public override Task<IGenericResult<MutationResult>> Translate(
        RestoreSnapshotCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.SnapshotId))
            return Task.FromResult<IGenericResult<MutationResult>>(
                GenericResult<MutationResult>.Failure(SqlResultCodes.SnapshotIdRequired));

        var restored = workspace.RestoreSnapshot(command.SnapshotId);
        if (!restored.IsSuccess)
            return Task.FromResult<IGenericResult<MutationResult>>(
                GenericResult<MutationResult>.Failure(SqlResultCodes.CommandExecutionFailed,
                    ResultDetails.Create("Message", restored.CurrentMessage ?? "Snapshot not found")));

        return Task.FromResult<IGenericResult<MutationResult>>(
            GenericResult<MutationResult>.Success(
                new MutationResult($"Restored snapshot '{command.SnapshotId}'", workspace.ScriptPaths)));
    }
}
