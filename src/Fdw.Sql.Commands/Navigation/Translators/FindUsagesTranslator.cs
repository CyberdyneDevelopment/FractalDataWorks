using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions;
using Fdw.Sql.Commands.Abstractions.Results;
using Fdw.Sql.Commands.Navigation.Commands;
using Fdw.Sql.Workspace;

namespace Fdw.Sql.Commands.Navigation.Translators;

[TypeOption(typeof(SqlNavigationTranslators), "FindUsages", RestrictToCurrentCompilation = true)]
public sealed class FindUsagesTranslator : SqlCommandTranslatorBase<FindUsagesCommand, QueryResult<string>>
{
    public FindUsagesTranslator() : base("FindUsages", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(FindUsagesCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(SqlResultCodes.NotYetImplemented));
}
