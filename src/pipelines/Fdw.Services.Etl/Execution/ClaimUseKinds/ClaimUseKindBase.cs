using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Etl.Execution;

/// <summary>Base class for the kinds of thing a pipeline writes whose container a claim can name.</summary>
public abstract class ClaimUseKindBase : TypeOptionBase<ClaimUseKindBase>, IClaimUseKind
{
    /// <summary>Initializes a new instance of the <see cref="ClaimUseKindBase"/> class.</summary>
    /// <param name="name">The option name, the kind a pipeline's use names (<c>PipelineUseKinds</c>).</param>
    protected ClaimUseKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract Task<IGenericResult<LoadTarget>> Resolve(LoadTargetResolver targets, string owner, PipelineUse use, CancellationToken cancellationToken);
}
