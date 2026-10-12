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

[TypeOption(typeof(SqlWorkspaceTranslators), "RevertToBaseline", RestrictToCurrentCompilation = true)]
public sealed class RevertToBaselineTranslator : SqlCommandTranslatorBase<RevertToBaselineCommand, MutationResult>
{
    public RevertToBaselineTranslator() : base("RevertToBaseline", "Reverts to baseline state.") { }

    public override Task<IGenericResult<MutationResult>> Translate(
        RevertToBaselineCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<RevertToBaselineTranslator>.Instance;
        RevertToBaselineTranslatorLog.Translating(logger);

        var n = workspace.RevertToBaseline();
        RevertToBaselineTranslatorLog.Reverted(logger, n);
        return Task.FromResult<IGenericResult<MutationResult>>(
            GenericResult<MutationResult>.Success(
                new MutationResult($"Reverted {n} script(s) to baseline", workspace.ScriptPaths)));
    }
}
