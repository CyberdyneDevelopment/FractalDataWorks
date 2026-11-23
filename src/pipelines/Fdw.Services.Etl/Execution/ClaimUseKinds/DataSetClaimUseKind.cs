using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Etl.Execution;

/// <summary>A data set: found by its name.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ClaimUseKinds), "DataSet")]
public sealed class DataSetClaimUseKind : ClaimUseKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetClaimUseKind"/> class.</summary>
    public DataSetClaimUseKind()
        : base(PipelineUseKinds.DataSet)
    {
    }

    /// <inheritdoc />
    public override Task<IGenericResult<LoadTarget>> Resolve(LoadTargetResolver targets, string owner, PipelineUse use, CancellationToken cancellationToken)
        => targets.ForDataSet(owner, use.Name, cancellationToken);
}
