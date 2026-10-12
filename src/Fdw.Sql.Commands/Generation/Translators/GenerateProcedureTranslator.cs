using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions;
using Fdw.Sql.Commands.Abstractions.Results;
using Fdw.Sql.Commands.Generation.Commands;
using Fdw.Sql.Commands.Logging;
using Fdw.Sql.Workspace;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Sql.Commands.Generation.Translators;

[TypeOption(typeof(SqlGenerationTranslators), "GenerateProcedure", RestrictToCurrentCompilation = true)]
public sealed class GenerateProcedureTranslator : SqlCommandTranslatorBase<GenerateProcedureCommand, QueryResult<string>>
{
    public GenerateProcedureTranslator() : base("GenerateProcedure", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(GenerateProcedureCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<GenerateProcedureTranslator>.Instance;
        GenerateProcedureTranslatorLog.Translating(logger, nameof(GenerateProcedureCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(GenerateProcedureTranslatorLog.NotYetImplemented(logger, nameof(GenerateProcedureCommand))));
    }
}
