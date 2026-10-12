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

[TypeOption(typeof(SqlBuildTranslators), "BuildProject", RestrictToCurrentCompilation = true)]
public sealed class BuildProjectTranslator : SqlCommandTranslatorBase<BuildProjectCommand, QueryResult<string>>
{
    public BuildProjectTranslator() : base("BuildProject", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(BuildProjectCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<BuildProjectTranslator>.Instance;
        BuildProjectTranslatorLog.Translating(logger, nameof(BuildProjectCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(BuildProjectTranslatorLog.NotYetImplemented(logger, nameof(BuildProjectCommand))));
    }
}
