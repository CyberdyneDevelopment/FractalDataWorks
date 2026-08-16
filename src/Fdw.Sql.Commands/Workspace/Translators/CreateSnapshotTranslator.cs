using System;
using System.Collections.Generic;
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

/// <summary>
/// Generates a placeholder snapshot ID. The handler intercepts the result and
/// patches the real SnapshotId returned by ISqlWorkspace.CreateSnapshot —
/// mirrors RoslynCommandHandler's CreateSnapshot patching.
/// </summary>
[TypeOption(typeof(SqlWorkspaceTranslators), "CreateSnapshot", RestrictToCurrentCompilation = true)]
public sealed class CreateSnapshotTranslator : SqlCommandTranslatorBase<CreateSnapshotCommand, QueryResult<SnapshotInfo>>
{
    public CreateSnapshotTranslator() : base("CreateSnapshot", "Captures a named snapshot.") { }

    public override Task<IGenericResult<QueryResult<SnapshotInfo>>> Translate(
        CreateSnapshotCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<CreateSnapshotTranslator>.Instance;
        CreateSnapshotTranslatorLog.Translating(logger, command.SnapshotName ?? string.Empty);

        if (string.IsNullOrWhiteSpace(command.SnapshotName))
            return Task.FromResult<IGenericResult<QueryResult<SnapshotInfo>>>(
                GenericResult<QueryResult<SnapshotInfo>>.Failure(CreateSnapshotTranslatorLog.SnapshotNameRequired(logger)));

        var placeholderId = Guid.NewGuid().ToString("N");
        var info = new SnapshotInfo { SnapshotId = placeholderId, Name = command.SnapshotName, Description = command.SnapshotDescription, ScriptCount = workspace.ScriptPaths.Count };
        CreateSnapshotTranslatorLog.SnapshotCreated(logger, command.SnapshotName, placeholderId);
        return Task.FromResult<IGenericResult<QueryResult<SnapshotInfo>>>(
            GenericResult<QueryResult<SnapshotInfo>>.Success(new QueryResult<SnapshotInfo>($"Created snapshot '{command.SnapshotName}'", info)));
    }
}
