using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// A relative output path was given but the solution has no file path to resolve it against.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "RelativeOutputPathNeedsSolutionPath", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RelativeOutputPathNeedsSolutionPathCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RelativeOutputPathNeedsSolutionPathCode"/> class.
    /// </summary>
    public RelativeOutputPathNeedsSolutionPathCode()
        : base(31026, "RelativeOutputPathNeedsSolutionPath",
            ResultSeverities.ByName("Error"),
            "Relative output path '{OutputPath}' cannot be resolved because the loaded solution has no file path; pass an absolute path instead",
            isRetryable: false)
    {
    }
}
