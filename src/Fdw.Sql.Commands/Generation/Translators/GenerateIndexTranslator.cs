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

[TypeOption(typeof(SqlGenerationTranslators), "GenerateIndex", RestrictToCurrentCompilation = true)]
public sealed class GenerateIndexTranslator : SqlCommandTranslatorBase<GenerateIndexCommand, QueryResult<string>>
{
    public GenerateIndexTranslator() : base("GenerateIndex", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(GenerateIndexCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<GenerateIndexTranslator>.Instance;
        GenerateIndexTranslatorLog.Translating(logger, nameof(GenerateIndexCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(GenerateIndexTranslatorLog.NotYetImplemented(logger, nameof(GenerateIndexCommand))));
    }
}
