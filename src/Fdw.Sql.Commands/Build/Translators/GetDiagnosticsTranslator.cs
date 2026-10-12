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

[TypeOption(typeof(SqlBuildTranslators), "GetDiagnostics", RestrictToCurrentCompilation = true)]
public sealed class GetDiagnosticsTranslator : SqlCommandTranslatorBase<GetDiagnosticsCommand, QueryResult<string>>
{
    public GetDiagnosticsTranslator() : base("GetDiagnostics", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(GetDiagnosticsCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<GetDiagnosticsTranslator>.Instance;
        GetDiagnosticsTranslatorLog.Translating(logger, nameof(GetDiagnosticsCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(GetDiagnosticsTranslatorLog.NotYetImplemented(logger, nameof(GetDiagnosticsCommand))));
    }
}
