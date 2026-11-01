using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// No guide's implemented types are reachable from the target.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "NoImplementationGuide", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoImplementationGuideCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoImplementationGuideCode"/> class.
    /// </summary>
    public NoImplementationGuideCode()
        : base(31037, "NoImplementationGuide",
            ResultSeverities.ByName("Error"),
            "No implementation guide applies to {Target}",
            isRetryable: false)
    {
    }
}
