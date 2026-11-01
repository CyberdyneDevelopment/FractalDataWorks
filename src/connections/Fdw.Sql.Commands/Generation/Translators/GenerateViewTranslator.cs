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

[TypeOption(typeof(SqlGenerationTranslators), "GenerateView", RestrictToCurrentCompilation = true)]
public sealed class GenerateViewTranslator : SqlCommandTranslatorBase<GenerateViewCommand, QueryResult<string>>
{
    public GenerateViewTranslator() : base("GenerateView", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(GenerateViewCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<GenerateViewTranslator>.Instance;
        GenerateViewTranslatorLog.Translating(logger, nameof(GenerateViewCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(GenerateViewTranslatorLog.NotYetImplemented(logger, nameof(GenerateViewCommand))));
    }
}
