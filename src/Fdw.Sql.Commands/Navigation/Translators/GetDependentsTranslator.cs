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

[TypeOption(typeof(SqlNavigationTranslators), "GetDependents", RestrictToCurrentCompilation = true)]
public sealed class GetDependentsTranslator : SqlCommandTranslatorBase<GetDependentsCommand, QueryResult<string>>
{
    public GetDependentsTranslator() : base("GetDependents", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(GetDependentsCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<GetDependentsTranslator>.Instance;
        GetDependentsTranslatorLog.Translating(logger, nameof(GetDependentsCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(GetDependentsTranslatorLog.NotYetImplemented(logger, nameof(GetDependentsCommand))));
    }
}
