using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Etl.Results;

/// <summary>
/// Type conversion failed during transform.
/// </summary>
[TypeOption(typeof(EtlResultCodes), "TypeConversionFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TypeConversionFailedCode : EtlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeConversionFailedCode"/> class.
    /// </summary>
    public TypeConversionFailedCode()
        : base(90002, "TypeConversionFailed",
            ResultSeverities.ByName("Error"),
            "Type conversion failed: {Message}",
            isRetryable: false)
    {
    }
}