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

[TypeOption(typeof(SqlNavigationTranslators), "FindDefinition", RestrictToCurrentCompilation = true)]
public sealed class FindDefinitionTranslator : SqlCommandTranslatorBase<FindDefinitionCommand, QueryResult<string>>
{
    public FindDefinitionTranslator() : base("FindDefinition", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(FindDefinitionCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<FindDefinitionTranslator>.Instance;
        FindDefinitionTranslatorLog.Translating(logger, nameof(FindDefinitionCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(FindDefinitionTranslatorLog.NotYetImplemented(logger, nameof(FindDefinitionCommand))));
    }
}
