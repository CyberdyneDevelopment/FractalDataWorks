using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Default value type is not compatible with parameter type.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "ParameterDefaultTypeMismatch", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ParameterDefaultTypeMismatchCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParameterDefaultTypeMismatchCode"/> class.
    /// </summary>
    public ParameterDefaultTypeMismatchCode()
        : base(21020, "ParameterDefaultTypeMismatch",
            ResultSeverities.ByName("Error"),
            "Default value type '{DefaultValueType}' is not compatible with parameter type '{ParameterType}'",
            isRetryable: false)
    {
    }
}