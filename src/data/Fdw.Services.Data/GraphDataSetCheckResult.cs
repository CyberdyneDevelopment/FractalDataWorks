using System.Collections.Generic;

namespace Fdw.Services.Data;

/// <summary>What the check before save found in a Graph data set draft (FDW-5290): every problem, and the intended sources not bound yet.</summary>
/// <param name="Problems">Every problem that would refuse the save; empty when the draft would be accepted.</param>
/// <param name="NotBound">The intended sources, each reported as not bound yet; they never block.</param>
/// <param name="Notes">What the check could not judge (FDW-5522), each a coded note such as <c>GraphDataSetFedFieldNotChecked</c>; they never block and are not read by save.</param>
public sealed record GraphDataSetCheckResult(IReadOnlyList<GraphDataSetProblem> Problems, IReadOnlyList<GraphDataSetNotBoundSource> NotBound, IReadOnlyList<GraphDataSetProblem> Notes)
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetCheckResult"/> record with no notes.</summary>
    /// <param name="problems">Every problem that would refuse the save.</param>
    /// <param name="notBound">The intended sources, each not bound yet.</param>
    public GraphDataSetCheckResult(IReadOnlyList<GraphDataSetProblem> problems, IReadOnlyList<GraphDataSetNotBoundSource> notBound)
        : this(problems, notBound, [])
    {
    }

    /// <summary>Gets a value indicating whether saving the draft would be accepted.</summary>
    public bool IsValid => Problems.Count == 0;
}
