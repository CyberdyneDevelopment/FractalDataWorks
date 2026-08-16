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

[TypeOption(typeof(SqlSearchTranslators), "SearchSymbols", RestrictToCurrentCompilation = true)]
public sealed class SearchSymbolsTranslator : SqlCommandTranslatorBase<SearchSymbolsCommand, QueryResult<string>>
{
    public SearchSymbolsTranslator() : base("SearchSymbols", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(SearchSymbolsCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<SearchSymbolsTranslator>.Instance;
        SearchSymbolsTranslatorLog.Translating(logger, nameof(SearchSymbolsCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(SearchSymbolsTranslatorLog.NotYetImplemented(logger, nameof(SearchSymbolsCommand))));
    }
}
