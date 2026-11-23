using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution;

/// <summary>A failure that carries no coded failure message in a known category.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RunFailureKinds), "Unclassified")]
public sealed class UnclassifiedRunFailureKind : RunFailureKindBase
{
    /// <summary>Initializes a new instance of the <see cref="UnclassifiedRunFailureKind"/> class.</summary>
    public UnclassifiedRunFailureKind()
        : base("Unclassified", false, false)
    {
    }

    /// <inheritdoc />
    public override string Describe(RunFailureClassification failure) => "The run failed with a failure that carries no coded category";
}
