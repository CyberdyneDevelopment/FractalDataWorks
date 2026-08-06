using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// The directive is supplied by MSBuild, so deleting the source line changes nothing.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "GlobalUsingIsMsBuildDuplicate", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GlobalUsingIsMsBuildDuplicateCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalUsingIsMsBuildDuplicateCode"/> class.
    /// </summary>
    public GlobalUsingIsMsBuildDuplicateCode()
        : base(31033, "GlobalUsingIsMsBuildDuplicate",
            ResultSeverities.ByName("Error"),
            "Global using '{Namespace}' in project '{Project}' is ALSO supplied by MSBuild (ImplicitUsings/<Using Include>), so deleting the source line changes nothing — the SDK regenerates it. Edit {PropsHint} instead",
            isRetryable: false)
    {
    }
}
