using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions;
using Fdw.Sql.Commands.Abstractions.Results;
using Fdw.Sql.Commands.Build.Commands;
using Fdw.Sql.Commands.Logging;
using Fdw.Sql.Workspace;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Sql.Commands.Build.Translators;

[TypeOption(typeof(SqlBuildTranslators), "GetCompilationOptions", RestrictToCurrentCompilation = true)]
public sealed class GetCompilationOptionsTranslator : SqlCommandTranslatorBase<GetCompilationOptionsCommand, QueryResult<string>>
{
    public GetCompilationOptionsTranslator() : base("GetCompilationOptions", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(GetCompilationOptionsCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<GetCompilationOptionsTranslator>.Instance;
        GetCompilationOptionsTranslatorLog.Translating(logger, nameof(GetCompilationOptionsCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(GetCompilationOptionsTranslatorLog.NotYetImplemented(logger, nameof(GetCompilationOptionsCommand))));
    }
}
