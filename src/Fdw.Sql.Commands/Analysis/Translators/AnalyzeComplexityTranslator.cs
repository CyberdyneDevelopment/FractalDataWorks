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

[TypeOption(typeof(SqlAnalysisTranslators), "AnalyzeComplexity", RestrictToCurrentCompilation = true)]
public sealed class AnalyzeComplexityTranslator : SqlCommandTranslatorBase<AnalyzeComplexityCommand, QueryResult<string>>
{
    public AnalyzeComplexityTranslator() : base("AnalyzeComplexity", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(AnalyzeComplexityCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<AnalyzeComplexityTranslator>.Instance;
        AnalyzeComplexityTranslatorLog.Translating(logger, nameof(AnalyzeComplexityCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(AnalyzeComplexityTranslatorLog.NotYetImplemented(logger, nameof(AnalyzeComplexityCommand))));
    }
}
