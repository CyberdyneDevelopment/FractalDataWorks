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

[TypeOption(typeof(SqlSearchTranslators), "FindUnused", RestrictToCurrentCompilation = true)]
public sealed class FindUnusedTranslator : SqlCommandTranslatorBase<FindUnusedCommand, QueryResult<string>>
{
    public FindUnusedTranslator() : base("FindUnused", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(FindUnusedCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<FindUnusedTranslator>.Instance;
        FindUnusedTranslatorLog.Translating(logger, nameof(FindUnusedCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(FindUnusedTranslatorLog.NotYetImplemented(logger, nameof(FindUnusedCommand))));
    }
}
