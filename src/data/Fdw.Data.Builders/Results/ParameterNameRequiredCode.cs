using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Parameter name is required.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "ParameterNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ParameterNameRequiredCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParameterNameRequiredCode"/> class.
    /// </summary>
    public ParameterNameRequiredCode()
        : base(21017, "ParameterNameRequired",
            ResultSeverities.ByName("Error"),
            "Parameter name is required",
            isRetryable: false)
    {
    }
}