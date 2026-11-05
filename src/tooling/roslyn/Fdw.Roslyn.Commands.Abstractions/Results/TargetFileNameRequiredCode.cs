using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Target file name is required.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "TargetFileNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TargetFileNameRequiredCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TargetFileNameRequiredCode"/> class.
    /// </summary>
    public TargetFileNameRequiredCode()
        : base(21027, "TargetFileNameRequired",
            ResultSeverities.ByName("Error"),
            "Target file name is required",
            isRetryable: false)
    {
    }
}
