using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions;
using Fdw.Sql.Commands.Abstractions.Results;
using Fdw.Sql.Commands.Project.Commands;
using Fdw.Sql.Workspace;

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
        if (string.IsNullOrWhiteSpace(command.FilePath))
            return Task.FromResult<IGenericResult<MutationResult>>(
                GenericResult<MutationResult>.Failure(SqlResultCodes.FilePathRequired));

        workspace.UpdateScript(command.FilePath, command.Content ?? string.Empty);
        return Task.FromResult<IGenericResult<MutationResult>>(
            GenericResult<MutationResult>.Success(
                new MutationResult($"Added script '{command.FilePath}'", new List<string> { command.FilePath })));
    }
}
