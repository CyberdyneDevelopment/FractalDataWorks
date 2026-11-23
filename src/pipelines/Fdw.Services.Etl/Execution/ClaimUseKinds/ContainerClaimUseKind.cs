using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Etl.Execution;

/// <summary>A container in a data store: found by its data store, path and name.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ClaimUseKinds), "Container")]
public sealed class ContainerClaimUseKind : ClaimUseKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerClaimUseKind"/> class.</summary>
    public ContainerClaimUseKind()
        : base(PipelineUseKinds.Container)
    {
    }

    /// <inheritdoc />
    public override Task<IGenericResult<LoadTarget>> Resolve(LoadTargetResolver targets, string owner, PipelineUse use, CancellationToken cancellationToken)
        => targets.ForContainer(
            owner, use.Parents.Count > 0 ? use.Parents[0] : string.Empty, use.Parents.Count > 1 ? use.Parents[1] : string.Empty, use.Name, cancellationToken);
}
