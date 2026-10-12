using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Parameter has incorrect type.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "ParameterTypeMismatch", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ParameterTypeMismatchCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParameterTypeMismatchCode"/> class.
    /// </summary>
    public ParameterTypeMismatchCode()
        : base(21023, "ParameterTypeMismatch",
            ResultSeverities.ByName("Error"),
            "Parameter '{ParameterName}' has type '{ActualType}' but expected '{ExpectedType}'",
            isRetryable: false)
    {
    }
}