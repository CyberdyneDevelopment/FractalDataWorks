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

[TypeOption(typeof(SqlGenerationTranslators), "GenerateTests", RestrictToCurrentCompilation = true)]
public sealed class GenerateTestsTranslator : SqlCommandTranslatorBase<GenerateTestsCommand, QueryResult<string>>
{
    public GenerateTestsTranslator() : base("GenerateTests", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(GenerateTestsCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<GenerateTestsTranslator>.Instance;
        GenerateTestsTranslatorLog.Translating(logger, nameof(GenerateTestsCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(GenerateTestsTranslatorLog.NotYetImplemented(logger, nameof(GenerateTestsCommand))));
    }
}
