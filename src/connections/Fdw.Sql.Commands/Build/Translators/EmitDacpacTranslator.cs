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

[TypeOption(typeof(SqlBuildTranslators), "EmitDacpac", RestrictToCurrentCompilation = true)]
public sealed class EmitDacpacTranslator : SqlCommandTranslatorBase<EmitDacpacCommand, QueryResult<string>>
{
    public EmitDacpacTranslator() : base("EmitDacpac", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(EmitDacpacCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<EmitDacpacTranslator>.Instance;
        EmitDacpacTranslatorLog.Translating(logger, nameof(EmitDacpacCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(EmitDacpacTranslatorLog.NotYetImplemented(logger, nameof(EmitDacpacCommand))));
    }
}
