using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions;
using Fdw.Sql.Commands.Abstractions.Results;
using Fdw.Sql.Commands.Analysis.Commands;
using Fdw.Sql.Commands.Logging;
using Fdw.Sql.Workspace;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Sql.Commands.Analysis.Translators;

[TypeOption(typeof(SqlAnalysisTranslators), "AnalyzeDependencies", RestrictToCurrentCompilation = true)]
public sealed class AnalyzeDependenciesTranslator : SqlCommandTranslatorBase<AnalyzeDependenciesCommand, QueryResult<string>>
{
    public AnalyzeDependenciesTranslator() : base("AnalyzeDependencies", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(AnalyzeDependenciesCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<AnalyzeDependenciesTranslator>.Instance;
        AnalyzeDependenciesTranslatorLog.Translating(logger, nameof(AnalyzeDependenciesCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(AnalyzeDependenciesTranslatorLog.NotYetImplemented(logger, nameof(AnalyzeDependenciesCommand))));
    }
}
