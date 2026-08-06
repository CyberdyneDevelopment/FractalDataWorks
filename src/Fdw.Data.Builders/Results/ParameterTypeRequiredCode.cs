using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Parameter type is required.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "ParameterTypeRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ParameterTypeRequiredCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParameterTypeRequiredCode"/> class.
    /// </summary>
    public ParameterTypeRequiredCode()
        : base(21018, "ParameterTypeRequired",
            ResultSeverities.ByName("Error"),
            "Parameter type is required",
            isRetryable: false)
    {
    }
}