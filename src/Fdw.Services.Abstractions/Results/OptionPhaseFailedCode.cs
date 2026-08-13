using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.ServiceTypes.Results;

/// <summary>
/// A single ServiceTypeOption's registration phase did not complete.
/// </summary>
/// <remarks>
/// Carried when the option's own phase body threw. An option that fails deliberately returns its own
/// domain's code instead — this one names the option, its position and the phase, so a failure with
/// no better code still says which option in which collect produced it.
/// </remarks>
[TypeOption(typeof(ServiceTypeResultCodes), "OptionPhaseFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OptionPhaseFailedCode : ServiceTypeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OptionPhaseFailedCode"/> class.
    /// </summary>
    // Why 61012: ServiceTypeLog.OptionPhaseFailed is EventId 61012.
    public OptionPhaseFailedCode()
        : base(61012, "OptionPhaseFailed",
            ResultSeverities.ByName("Error"),
            "[{OptionName}] {Phase} (option #{Ordinal} in {CollectionName}) FAILED while running the {Implementation} implementation: {ErrorMessage}",
            isRetryable: false)
    {
    }
}
