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

[TypeOption(typeof(SqlNavigationTranslators), "GetObjectSchema", RestrictToCurrentCompilation = true)]
public sealed class GetObjectSchemaTranslator : SqlCommandTranslatorBase<GetObjectSchemaCommand, QueryResult<string>>
{
    public GetObjectSchemaTranslator() : base("GetObjectSchema", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(GetObjectSchemaCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<GetObjectSchemaTranslator>.Instance;
        GetObjectSchemaTranslatorLog.Translating(logger, nameof(GetObjectSchemaCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(GetObjectSchemaTranslatorLog.NotYetImplemented(logger, nameof(GetObjectSchemaCommand))));
    }
}
