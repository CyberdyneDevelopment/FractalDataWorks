using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Projects.Abstractions.TypeCollections;

/// <summary>
/// When a Stage fails, continue executing subsequent stages in the Project.
/// The Project itself records failure at completion but does not short-circuit remaining stages.
/// This is the less strict option (ContinueProject &lt; HaltProject).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(StageFailurePolicies), "ContinueProject")]
public sealed class ContinueProjectPolicy : StageFailurePolicyBase
{
    /// <summary>Initializes a new instance of the <see cref="ContinueProjectPolicy"/> class.</summary>
    public ContinueProjectPolicy() : base(2, "ContinueProject")
    {
    }
}
