using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution;

/// <summary>A failure in a known category that counts toward a failure threshold.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RunFailureKinds), "ClassifiedCounted")]
public sealed class ClassifiedCountedRunFailureKind : RunFailureKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ClassifiedCountedRunFailureKind"/> class.</summary>
    public ClassifiedCountedRunFailureKind()
        : base("ClassifiedCounted", true, false)
    {
    }

    /// <inheritdoc />
    public override string Describe(RunFailureClassification failure) => $"The run failed with a {failure.Category} failure ({failure.Code}), which is {(failure.Transient ? "transient" : "not transient")}";
}
