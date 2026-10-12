using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions;
using Fdw.Sql.Commands.Abstractions.Results;
using Fdw.Sql.Commands.Refactoring.Commands;
using Fdw.Sql.Commands.Logging;
using Fdw.Sql.Workspace;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Sql.Commands.Refactoring.Translators;

[TypeOption(typeof(SqlRefactoringTranslators), "InlineVariable", RestrictToCurrentCompilation = true)]
public sealed class InlineVariableTranslator : SqlCommandTranslatorBase<InlineVariableCommand, QueryResult<string>>
{
    public InlineVariableTranslator() : base("InlineVariable", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(InlineVariableCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<InlineVariableTranslator>.Instance;
        InlineVariableTranslatorLog.Translating(logger, nameof(InlineVariableCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(InlineVariableTranslatorLog.NotYetImplemented(logger, nameof(InlineVariableCommand))));
    }
}
