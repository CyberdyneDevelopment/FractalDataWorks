using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Aegis.Abstractions;

/// <summary>
/// A submitted parameter is absent from the command's <c>ParameterAllowList</c>, or its value is not
/// one of the permitted values. Caller-input validation failure.
/// </summary>
[TypeOption(typeof(AegisResultCodes), "ParameterNotInAllowList", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ParameterNotInAllowListCode : AegisResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParameterNotInAllowListCode"/> class.
    /// </summary>
    public ParameterNotInAllowListCode()
        : base(21000, "ParameterNotInAllowList",
            ResultSeverities.ByName("Error"),
            "Parameter '{parameterName}' is not permitted for command '{commandName}'.",
            isRetryable: false)
    {
    }
}
