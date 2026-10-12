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

[TypeOption(typeof(SqlGenerationTranslators), "GenerateTable", RestrictToCurrentCompilation = true)]
public sealed class GenerateTableTranslator : SqlCommandTranslatorBase<GenerateTableCommand, QueryResult<string>>
{
    public GenerateTableTranslator() : base("GenerateTable", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(GenerateTableCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<GenerateTableTranslator>.Instance;
        GenerateTableTranslatorLog.Translating(logger, nameof(GenerateTableCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(GenerateTableTranslatorLog.NotYetImplemented(logger, nameof(GenerateTableCommand))));
    }
}
