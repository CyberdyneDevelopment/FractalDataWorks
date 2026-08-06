using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataContainers.Abstractions.Results;

/// <summary>
/// Cannot convert field value to target type.
/// </summary>
[TypeOption(typeof(DataContainerResultCodes), "FieldConversionFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FieldConversionFailedCode : DataContainerResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldConversionFailedCode"/> class.
    /// </summary>
    public FieldConversionFailedCode()
        : base(90002, "FieldConversionFailed",
            ResultSeverities.ByName("Error"),
            "Cannot convert value to {TargetType}: {ErrorMessage}",
            isRetryable: false)
    {
    }
}