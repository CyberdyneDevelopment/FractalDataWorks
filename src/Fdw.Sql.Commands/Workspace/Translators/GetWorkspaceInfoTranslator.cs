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

[TypeOption(typeof(SqlWorkspaceTranslators), "GetWorkspaceInfo", RestrictToCurrentCompilation = true)]
public sealed class GetWorkspaceInfoTranslator : SqlCommandTranslatorBase<GetWorkspaceInfoCommand, QueryResult<WorkspaceInfo>>
{
    public GetWorkspaceInfoTranslator() : base("GetWorkspaceInfo", "Returns workspace metadata.") { }

    public override Task<IGenericResult<QueryResult<WorkspaceInfo>>> Translate(
        GetWorkspaceInfoCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<GetWorkspaceInfoTranslator>.Instance;
        GetWorkspaceInfoTranslatorLog.Translating(logger);

        var info = new WorkspaceInfo
        {
            ProjectPath = workspace.ProjectPath,
            ScriptCount = workspace.ScriptPaths.Count,
            HasBaseline = workspace.BaselineModel is not null,
        };
        GetWorkspaceInfoTranslatorLog.WorkspaceSummarized(logger, info.ProjectPath, info.ScriptCount, info.HasBaseline);
        return Task.FromResult<IGenericResult<QueryResult<WorkspaceInfo>>>(
            GenericResult<QueryResult<WorkspaceInfo>>.Success(
                new QueryResult<WorkspaceInfo>(
                    $"Workspace at '{System.IO.Path.GetFileName(info.ProjectPath)}' ({info.ScriptCount} scripts)",
                    info)));
    }
}
