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

[TypeOption(typeof(SqlProjectTranslators), "AddScript", RestrictToCurrentCompilation = true)]
public sealed class AddScriptTranslator : SqlCommandTranslatorBase<AddScriptCommand, MutationResult>
{
    public AddScriptTranslator() : base("AddScript", "Adds a script to the workspace in memory.") { }

    public override Task<IGenericResult<MutationResult>> Translate(
        AddScriptCommand command,
        ISqlWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<AddScriptTranslator>.Instance;
        AddScriptTranslatorLog.Translating(logger, command.FilePath ?? string.Empty);

        if (string.IsNullOrWhiteSpace(command.FilePath))
            return Task.FromResult<IGenericResult<MutationResult>>(
                GenericResult<MutationResult>.Failure(AddScriptTranslatorLog.FilePathRequired(logger)));

        workspace.UpdateScript(command.FilePath, command.Content ?? string.Empty);
        AddScriptTranslatorLog.ScriptAdded(logger, command.FilePath);
        return Task.FromResult<IGenericResult<MutationResult>>(
            GenericResult<MutationResult>.Success(
                new MutationResult($"Added script '{command.FilePath}'", new List<string> { command.FilePath })));
    }
}
