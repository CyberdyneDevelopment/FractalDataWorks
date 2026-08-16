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

[TypeOption(typeof(SqlGenerationTranslators), "GenerateMigration", RestrictToCurrentCompilation = true)]
public sealed class GenerateMigrationTranslator : SqlCommandTranslatorBase<GenerateMigrationCommand, QueryResult<string>>
{
    public GenerateMigrationTranslator() : base("GenerateMigration", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(GenerateMigrationCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<GenerateMigrationTranslator>.Instance;
        GenerateMigrationTranslatorLog.Translating(logger, nameof(GenerateMigrationCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(GenerateMigrationTranslatorLog.NotYetImplemented(logger, nameof(GenerateMigrationCommand))));
    }
}
