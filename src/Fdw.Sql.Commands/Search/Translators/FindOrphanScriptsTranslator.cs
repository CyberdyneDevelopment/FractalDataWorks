using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions;
using Fdw.Sql.Commands.Abstractions.Results;
using Fdw.Sql.Commands.Search.Commands;
using Fdw.Sql.Commands.Logging;
using Fdw.Sql.Workspace;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Sql.Commands.Search.Translators;

[TypeOption(typeof(SqlSearchTranslators), "FindOrphanScripts", RestrictToCurrentCompilation = true)]
public sealed class FindOrphanScriptsTranslator : SqlCommandTranslatorBase<FindOrphanScriptsCommand, QueryResult<string>>
{
    public FindOrphanScriptsTranslator() : base("FindOrphanScripts", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(FindOrphanScriptsCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<FindOrphanScriptsTranslator>.Instance;
        FindOrphanScriptsTranslatorLog.Translating(logger, nameof(FindOrphanScriptsCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(FindOrphanScriptsTranslatorLog.NotYetImplemented(logger, nameof(FindOrphanScriptsCommand))));
    }
}
