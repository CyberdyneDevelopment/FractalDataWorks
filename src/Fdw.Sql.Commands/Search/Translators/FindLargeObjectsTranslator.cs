using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions;
using Fdw.Sql.Commands.Abstractions.Results;
using Fdw.Sql.Commands.Search.Commands;
using Fdw.Sql.Workspace;

namespace Fdw.Sql.Commands.Search.Translators;

[TypeOption(typeof(SqlSearchTranslators), "FindLargeObjects", RestrictToCurrentCompilation = true)]
public sealed class FindLargeObjectsTranslator : SqlCommandTranslatorBase<FindLargeObjectsCommand, QueryResult<string>>
{
    public FindLargeObjectsTranslator() : base("FindLargeObjects", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(FindLargeObjectsCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(SqlResultCodes.NotYetImplemented));
}
