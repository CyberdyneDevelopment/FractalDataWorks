using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions;
using Fdw.Sql.Commands.Abstractions.Results;
using Fdw.Sql.Commands.Navigation.Commands;
using Fdw.Sql.Commands.Logging;
using Fdw.Sql.Workspace;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Sql.Commands.Navigation.Translators;

[TypeOption(typeof(SqlNavigationTranslators), "GetContainingObject", RestrictToCurrentCompilation = true)]
public sealed class GetContainingObjectTranslator : SqlCommandTranslatorBase<GetContainingObjectCommand, QueryResult<string>>
{
    public GetContainingObjectTranslator() : base("GetContainingObject", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(GetContainingObjectCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<GetContainingObjectTranslator>.Instance;
        GetContainingObjectTranslatorLog.Translating(logger, nameof(GetContainingObjectCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(GetContainingObjectTranslatorLog.NotYetImplemented(logger, nameof(GetContainingObjectCommand))));
    }
}
