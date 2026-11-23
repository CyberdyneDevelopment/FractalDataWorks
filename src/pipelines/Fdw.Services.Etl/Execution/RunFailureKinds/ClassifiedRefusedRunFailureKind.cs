using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution;

/// <summary>A failure in a known category that is a refusal.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RunFailureKinds), "ClassifiedRefused")]
public sealed class ClassifiedRefusedRunFailureKind : RunFailureKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ClassifiedRefusedRunFailureKind"/> class.</summary>
    public ClassifiedRefusedRunFailureKind()
        : base("ClassifiedRefused", true, true)
    {
    }

    /// <inheritdoc />
    public override string Describe(RunFailureClassification failure) => $"The run was refused with a {failure.Category} failure ({failure.Code}); a refusal is not the pipeline's own fault and is not counted";
}
