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

[TypeOption(typeof(SqlSearchTranslators), "SearchText", RestrictToCurrentCompilation = true)]
public sealed class SearchTextTranslator : SqlCommandTranslatorBase<SearchTextCommand, QueryResult<string>>
{
    public SearchTextTranslator() : base("SearchText", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(SearchTextCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<SearchTextTranslator>.Instance;
        SearchTextTranslatorLog.Translating(logger, nameof(SearchTextCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(SearchTextTranslatorLog.NotYetImplemented(logger, nameof(SearchTextCommand))));
    }
}
