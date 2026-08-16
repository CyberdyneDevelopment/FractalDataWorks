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

[TypeOption(typeof(SqlRefactoringTranslators), "MoveToSchema", RestrictToCurrentCompilation = true)]
public sealed class MoveToSchemaTranslator : SqlCommandTranslatorBase<MoveToSchemaCommand, QueryResult<string>>
{
    public MoveToSchemaTranslator() : base("MoveToSchema", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(MoveToSchemaCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<MoveToSchemaTranslator>.Instance;
        MoveToSchemaTranslatorLog.Translating(logger, nameof(MoveToSchemaCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(MoveToSchemaTranslatorLog.NotYetImplemented(logger, nameof(MoveToSchemaCommand))));
    }
}
