using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions;
using Fdw.Sql.Commands.Abstractions.Results;
using Fdw.Sql.Commands.Logging;
using Fdw.Sql.Commands.Project.Commands;
using Fdw.Sql.Workspace;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SqlServer.Dac.Model;

namespace Fdw.Sql.Commands.Project.Translators;

[TypeOption(typeof(SqlProjectTranslators), "GetProjectInfo", RestrictToCurrentCompilation = true)]
public sealed class GetProjectInfoTranslator : SqlCommandTranslatorBase<GetProjectInfoCommand, QueryResult<SqlProjectSummary>>
{
    public GetProjectInfoTranslator() : base("GetProjectInfo", "Returns workspace-level metadata.") { }

    public override Task<IGenericResult<QueryResult<SqlProjectSummary>>> Translate(
        GetProjectInfoCommand command,
        ISqlWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<GetProjectInfoTranslator>.Instance;
        GetProjectInfoTranslatorLog.Translating(logger);

        var byKind = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var obj in workspace.Model.GetObjects(DacQueryScopes.Default))
        {
            var k = obj.ObjectType.Name;
            byKind[k] = byKind.TryGetValue(k, out var c) ? c + 1 : 1;
        }
        var summary = new SqlProjectSummary(
            ProjectPath: workspace.ProjectPath,
            ScriptCount: workspace.ScriptPaths.Count,
            TotalObjectCount: byKind.Values.Sum(),
            ObjectCountsByKind: byKind);

        GetProjectInfoTranslatorLog.ProjectSummarized(logger, summary.ProjectPath, summary.ScriptCount, summary.TotalObjectCount);
        return Task.FromResult<IGenericResult<QueryResult<SqlProjectSummary>>>(
            GenericResult<QueryResult<SqlProjectSummary>>.Success(
                new QueryResult<SqlProjectSummary>(
                    $"Project '{System.IO.Path.GetFileNameWithoutExtension(workspace.ProjectPath)}': {workspace.ScriptPaths.Count} scripts, {summary.TotalObjectCount} objects",
                    summary)));
    }
}
