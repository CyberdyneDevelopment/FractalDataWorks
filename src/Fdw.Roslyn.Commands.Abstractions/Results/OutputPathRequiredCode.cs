using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Output path is required.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "OutputPathRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OutputPathRequiredCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutputPathRequiredCode"/> class.
    /// </summary>
    public OutputPathRequiredCode()
        : base(21023, "OutputPathRequired",
            ResultSeverities.ByName("Error"),
            "Output path is required",
            isRetryable: false)
    {
    }
}
