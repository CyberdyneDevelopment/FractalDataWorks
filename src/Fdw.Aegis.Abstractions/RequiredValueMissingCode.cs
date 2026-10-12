using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Aegis.Abstractions;

/// <summary>
/// A required value was not provided. Reuses the FDW-reserved canonical Validation code (20000).
/// </summary>
[TypeOption(typeof(AegisResultCodes), "RequiredValueMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RequiredValueMissingCode : AegisResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiredValueMissingCode"/> class.
    /// </summary>
    public RequiredValueMissingCode()
        : base(20000, "RequiredValueMissing",
            ResultSeverities.ByName("Error"),
            "Required value '{name}' was not provided.",
            isRetryable: false)
    {
    }
}
