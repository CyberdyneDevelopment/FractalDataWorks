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

[TypeOption(typeof(SqlWorkspaceTranslators), "ApplyWorkspaceChanges", RestrictToCurrentCompilation = true)]
public sealed class ApplyWorkspaceChangesTranslator : SqlCommandTranslatorBase<ApplyWorkspaceChangesCommand, QueryResult<IReadOnlyList<string>>>
{
    public ApplyWorkspaceChangesTranslator() : base("ApplyWorkspaceChanges", "Writes pending edits to disk.") { }

    public override async Task<IGenericResult<QueryResult<IReadOnlyList<string>>>> Translate(
        ApplyWorkspaceChangesCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<ApplyWorkspaceChangesTranslator>.Instance;
        ApplyWorkspaceChangesTranslatorLog.Translating(logger);

        var applied = await workspace.ApplyChanges(cancellationToken).ConfigureAwait(false);
        if (!applied.IsSuccess)
            return GenericResult<QueryResult<IReadOnlyList<string>>>.Failure(
                ApplyWorkspaceChangesTranslatorLog.ApplyFailed(logger, applied.CurrentMessage ?? "Apply failed"));
        var paths = applied.Value ?? new List<string>();
        ApplyWorkspaceChangesTranslatorLog.ChangesApplied(logger, paths.Count);
        return GenericResult<QueryResult<IReadOnlyList<string>>>.Success(
            new QueryResult<IReadOnlyList<string>>($"Wrote {paths.Count} script(s) to disk", paths));
    }
}
