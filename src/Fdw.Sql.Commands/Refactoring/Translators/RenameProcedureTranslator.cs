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

[TypeOption(typeof(SqlRefactoringTranslators), "RenameProcedure", RestrictToCurrentCompilation = true)]
public sealed class RenameProcedureTranslator : SqlCommandTranslatorBase<RenameProcedureCommand, QueryResult<string>>
{
    public RenameProcedureTranslator() : base("RenameProcedure", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(RenameProcedureCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<RenameProcedureTranslator>.Instance;
        RenameProcedureTranslatorLog.Translating(logger, nameof(RenameProcedureCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(RenameProcedureTranslatorLog.NotYetImplemented(logger, nameof(RenameProcedureCommand))));
    }
}
