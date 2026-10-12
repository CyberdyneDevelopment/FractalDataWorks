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

[TypeOption(typeof(SqlWorkspaceTranslators), "SetBaseline", RestrictToCurrentCompilation = true)]
public sealed class SetBaselineTranslator : SqlCommandTranslatorBase<SetBaselineCommand, QueryResult<int>>
{
    public SetBaselineTranslator() : base("SetBaseline", "Marks current state as baseline.") { }

    public override Task<IGenericResult<QueryResult<int>>> Translate(
        SetBaselineCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<SetBaselineTranslator>.Instance;
        SetBaselineTranslatorLog.Translating(logger);

        workspace.SetBaseline();
        SetBaselineTranslatorLog.BaselineSet(logger, workspace.ScriptPaths.Count);
        return Task.FromResult<IGenericResult<QueryResult<int>>>(
            GenericResult<QueryResult<int>>.Success(
                new QueryResult<int>($"Baseline set with {workspace.ScriptPaths.Count} script(s)", workspace.ScriptPaths.Count)));
    }
}
