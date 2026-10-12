using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Selected code is not within a method.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "SelectedCodeNotWithinMethod", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SelectedCodeNotWithinMethodCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SelectedCodeNotWithinMethodCode"/> class.
    /// </summary>
    public SelectedCodeNotWithinMethodCode()
        : base(21016, "SelectedCodeNotWithinMethod",
            ResultSeverities.ByName("Error"),
            "Selected code is not within a method",
            isRetryable: false)
    {
    }
}
