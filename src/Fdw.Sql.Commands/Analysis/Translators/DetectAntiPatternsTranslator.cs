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

[TypeOption(typeof(SqlAnalysisTranslators), "DetectAntiPatterns", RestrictToCurrentCompilation = true)]
public sealed class DetectAntiPatternsTranslator : SqlCommandTranslatorBase<DetectAntiPatternsCommand, QueryResult<string>>
{
    public DetectAntiPatternsTranslator() : base("DetectAntiPatterns", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(DetectAntiPatternsCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<DetectAntiPatternsTranslator>.Instance;
        DetectAntiPatternsTranslatorLog.Translating(logger, nameof(DetectAntiPatternsCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(DetectAntiPatternsTranslatorLog.NotYetImplemented(logger, nameof(DetectAntiPatternsCommand))));
    }
}
