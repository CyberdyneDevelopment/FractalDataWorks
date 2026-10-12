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

[TypeOption(typeof(SqlAnalysisTranslators), "AnalyzeIndexCoverage", RestrictToCurrentCompilation = true)]
public sealed class AnalyzeIndexCoverageTranslator : SqlCommandTranslatorBase<AnalyzeIndexCoverageCommand, QueryResult<string>>
{
    public AnalyzeIndexCoverageTranslator() : base("AnalyzeIndexCoverage", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(AnalyzeIndexCoverageCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<AnalyzeIndexCoverageTranslator>.Instance;
        AnalyzeIndexCoverageTranslatorLog.Translating(logger, nameof(AnalyzeIndexCoverageCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(AnalyzeIndexCoverageTranslatorLog.NotYetImplemented(logger, nameof(AnalyzeIndexCoverageCommand))));
    }
}
