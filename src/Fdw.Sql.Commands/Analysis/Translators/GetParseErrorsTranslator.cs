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

[TypeOption(typeof(SqlAnalysisTranslators), "GetParseErrors", RestrictToCurrentCompilation = true)]
public sealed class GetParseErrorsTranslator : SqlCommandTranslatorBase<GetParseErrorsCommand, QueryResult<string>>
{
    public GetParseErrorsTranslator() : base("GetParseErrors", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(GetParseErrorsCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<GetParseErrorsTranslator>.Instance;
        GetParseErrorsTranslatorLog.Translating(logger, nameof(GetParseErrorsCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(GetParseErrorsTranslatorLog.NotYetImplemented(logger, nameof(GetParseErrorsCommand))));
    }
}
