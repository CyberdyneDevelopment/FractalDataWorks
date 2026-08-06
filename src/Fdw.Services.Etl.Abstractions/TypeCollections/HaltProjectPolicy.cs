using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Projects.Abstractions.TypeCollections;

/// <summary>
/// When a Stage fails, halt the entire Project immediately.
/// All subsequent stages are not executed.
/// This is the stricter option (HaltProject &gt; ContinueProject).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(StageFailurePolicies), "HaltProject")]
public sealed class HaltProjectPolicy : StageFailurePolicyBase
{
    /// <summary>Initializes a new instance of the <see cref="HaltProjectPolicy"/> class.</summary>
    public HaltProjectPolicy() : base(1, "HaltProject")
    {
    }
}
