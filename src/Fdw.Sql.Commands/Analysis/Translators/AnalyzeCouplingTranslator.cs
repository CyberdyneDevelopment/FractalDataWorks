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

[TypeOption(typeof(SqlAnalysisTranslators), "AnalyzeCoupling", RestrictToCurrentCompilation = true)]
public sealed class AnalyzeCouplingTranslator : SqlCommandTranslatorBase<AnalyzeCouplingCommand, QueryResult<string>>
{
    public AnalyzeCouplingTranslator() : base("AnalyzeCoupling", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(AnalyzeCouplingCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<AnalyzeCouplingTranslator>.Instance;
        AnalyzeCouplingTranslatorLog.Translating(logger, nameof(AnalyzeCouplingCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(AnalyzeCouplingTranslatorLog.NotYetImplemented(logger, nameof(AnalyzeCouplingCommand))));
    }
}
