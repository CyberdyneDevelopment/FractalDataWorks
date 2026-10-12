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

[TypeOption(typeof(SqlSearchTranslators), "FindDuplicates", RestrictToCurrentCompilation = true)]
public sealed class FindDuplicatesTranslator : SqlCommandTranslatorBase<FindDuplicatesCommand, QueryResult<string>>
{
    public FindDuplicatesTranslator() : base("FindDuplicates", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(FindDuplicatesCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<FindDuplicatesTranslator>.Instance;
        FindDuplicatesTranslatorLog.Translating(logger, nameof(FindDuplicatesCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(FindDuplicatesTranslatorLog.NotYetImplemented(logger, nameof(FindDuplicatesCommand))));
    }
}
