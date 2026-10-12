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

[TypeOption(typeof(SqlNavigationTranslators), "FindReferences", RestrictToCurrentCompilation = true)]
public sealed class FindReferencesTranslator : SqlCommandTranslatorBase<FindReferencesCommand, QueryResult<string>>
{
    public FindReferencesTranslator() : base("FindReferences", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(FindReferencesCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<FindReferencesTranslator>.Instance;
        FindReferencesTranslatorLog.Translating(logger, nameof(FindReferencesCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(FindReferencesTranslatorLog.NotYetImplemented(logger, nameof(FindReferencesCommand))));
    }
}
