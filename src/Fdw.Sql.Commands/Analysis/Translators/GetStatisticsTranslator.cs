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

[TypeOption(typeof(SqlAnalysisTranslators), "GetStatistics", RestrictToCurrentCompilation = true)]
public sealed class GetStatisticsTranslator : SqlCommandTranslatorBase<GetStatisticsCommand, QueryResult<string>>
{
    public GetStatisticsTranslator() : base("GetStatistics", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(GetStatisticsCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<GetStatisticsTranslator>.Instance;
        GetStatisticsTranslatorLog.Translating(logger, nameof(GetStatisticsCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(GetStatisticsTranslatorLog.NotYetImplemented(logger, nameof(GetStatisticsCommand))));
    }
}
