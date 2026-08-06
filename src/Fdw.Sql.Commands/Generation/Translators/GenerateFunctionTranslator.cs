using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions;
using Fdw.Sql.Commands.Abstractions.Results;
using Fdw.Sql.Commands.Generation.Commands;
using Fdw.Sql.Workspace;

namespace Fdw.Sql.Commands.Generation.Translators;

[TypeOption(typeof(SqlGenerationTranslators), "GenerateFunction", RestrictToCurrentCompilation = true)]
public sealed class GenerateFunctionTranslator : SqlCommandTranslatorBase<GenerateFunctionCommand, QueryResult<string>>
{
    public GenerateFunctionTranslator() : base("GenerateFunction", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(GenerateFunctionCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(SqlResultCodes.NotYetImplemented));
}
