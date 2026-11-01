using System.Collections.Generic;
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

namespace Fdw.Sql.Commands.Project.Translators;

[TypeOption(typeof(SqlProjectTranslators), "RemoveScript", RestrictToCurrentCompilation = true)]
public sealed class RemoveScriptTranslator : SqlCommandTranslatorBase<RemoveScriptCommand, MutationResult>
{
    public RemoveScriptTranslator() : base("RemoveScript", "Replaces a script with an empty body (in-memory).") { }

    public override Task<IGenericResult<MutationResult>> Translate(
        RemoveScriptCommand command,
        ISqlWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<RemoveScriptTranslator>.Instance;
        RemoveScriptTranslatorLog.Translating(logger, command.FilePath ?? string.Empty);

        if (string.IsNullOrWhiteSpace(command.FilePath))
            return Task.FromResult<IGenericResult<MutationResult>>(
                GenericResult<MutationResult>.Failure(RemoveScriptTranslatorLog.FilePathRequired(logger)));
        if (workspace.GetScriptText(command.FilePath) is null)
            return Task.FromResult<IGenericResult<MutationResult>>(
                GenericResult<MutationResult>.Failure(RemoveScriptTranslatorLog.ScriptNotFound(logger, command.FilePath)));

        workspace.UpdateScript(command.FilePath, string.Empty);
        RemoveScriptTranslatorLog.ScriptRemoved(logger, command.FilePath);
        return Task.FromResult<IGenericResult<MutationResult>>(
            GenericResult<MutationResult>.Success(
                new MutationResult($"Removed script '{command.FilePath}'", new List<string> { command.FilePath })));
    }
}
