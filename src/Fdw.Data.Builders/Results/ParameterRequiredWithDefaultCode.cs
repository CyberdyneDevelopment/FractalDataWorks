using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Required parameter cannot have a default value.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "ParameterRequiredWithDefault", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ParameterRequiredWithDefaultCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParameterRequiredWithDefaultCode"/> class.
    /// </summary>
    public ParameterRequiredWithDefaultCode()
        : base(21019, "ParameterRequiredWithDefault",
            ResultSeverities.ByName("Error"),
            "Required parameter '{ParameterName}' cannot have a default value. Mark it as optional or remove the default value",
            isRetryable: false)
    {
    }
}