using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Etl.Execution;

/// <summary>A kind of thing a pipeline writes whose container a claim can name, and how that container is found.</summary>
public interface IClaimUseKind : ITypeOption<int, ClaimUseKindBase>
{
    /// <summary>Finds the container a pipeline's write of this kind names.</summary>
    /// <param name="targets">Resolves a load target.</param>
    /// <param name="owner">Who is asking, for a failure message.</param>
    /// <param name="use">The write, of this kind.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The target, or the failure that says why it cannot be used.</returns>
    Task<IGenericResult<LoadTarget>> Resolve(LoadTargetResolver targets, string owner, PipelineUse use, CancellationToken cancellationToken);
}
