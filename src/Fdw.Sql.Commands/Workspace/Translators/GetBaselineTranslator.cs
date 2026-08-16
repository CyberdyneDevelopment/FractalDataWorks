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

[TypeOption(typeof(SqlWorkspaceTranslators), "GetBaseline", RestrictToCurrentCompilation = true)]
public sealed class GetBaselineTranslator : SqlCommandTranslatorBase<GetBaselineCommand, QueryResult<BaselineInfo>>
{
    public GetBaselineTranslator() : base("GetBaseline", "Returns baseline info.") { }

    public override Task<IGenericResult<QueryResult<BaselineInfo>>> Translate(
        GetBaselineCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<GetBaselineTranslator>.Instance;
        GetBaselineTranslatorLog.Translating(logger);

        var info = new BaselineInfo { HasBaseline = workspace.BaselineModel is not null, ScriptCount = workspace.ScriptPaths.Count };
        GetBaselineTranslatorLog.BaselineState(logger, info.HasBaseline);
        var msg = info.HasBaseline ? $"Baseline set ({info.ScriptCount} scripts)" : "No baseline has been set";
        GetBaselineTranslatorLog.BaselineReturned(logger, msg);
        return Task.FromResult<IGenericResult<QueryResult<BaselineInfo>>>(
            GenericResult<QueryResult<BaselineInfo>>.Success(new QueryResult<BaselineInfo>(msg, info)));
    }
}
