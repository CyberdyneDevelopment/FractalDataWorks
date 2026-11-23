using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution;

/// <summary>A refusal that carries no coded category.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RunFailureKinds), "UnclassifiedRefused")]
public sealed class UnclassifiedRefusedRunFailureKind : RunFailureKindBase
{
    /// <summary>Initializes a new instance of the <see cref="UnclassifiedRefusedRunFailureKind"/> class.</summary>
    public UnclassifiedRefusedRunFailureKind()
        : base("UnclassifiedRefused", false, true)
    {
    }

    /// <inheritdoc />
    public override string Describe(RunFailureClassification failure) => $"The run was refused ({failure.Code}); a refusal is not the pipeline's own fault and is not counted";
}
