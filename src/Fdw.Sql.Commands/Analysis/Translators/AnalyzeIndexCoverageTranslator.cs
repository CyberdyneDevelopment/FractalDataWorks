using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions;
using Fdw.Sql.Commands.Abstractions.Results;
using Fdw.Sql.Commands.Analysis.Commands;
using Fdw.Sql.Workspace;

namespace Fdw.Sql.Commands.Analysis.Translators;

[TypeOption(typeof(SqlAnalysisTranslators), "AnalyzeIndexCoverage", RestrictToCurrentCompilation = true)]
public sealed class AnalyzeIndexCoverageTranslator : SqlCommandTranslatorBase<AnalyzeIndexCoverageCommand, QueryResult<string>>
{
    public AnalyzeIndexCoverageTranslator() : base("AnalyzeIndexCoverage", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(AnalyzeIndexCoverageCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(SqlResultCodes.NotYetImplemented));
}
