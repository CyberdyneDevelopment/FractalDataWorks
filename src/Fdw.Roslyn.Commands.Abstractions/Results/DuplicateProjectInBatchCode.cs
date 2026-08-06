using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// A project appears more than once in a move batch.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "DuplicateProjectInBatch", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DuplicateProjectInBatchCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateProjectInBatchCode"/> class.
    /// </summary>
    public DuplicateProjectInBatchCode()
        : base(41000, "DuplicateProjectInBatch",
            ResultSeverities.ByName("Error"),
            "Project appears more than once in move batch: {ProjectName}",
            isRetryable: false)
    {
    }
}
