using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>
/// Invalid state transition attempted.
/// </summary>
[TypeOption(typeof(OperationsResultCodes), "InvalidStateTransition", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidStateTransitionCode : OperationsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidStateTransitionCode"/> class.
    /// </summary>
    public InvalidStateTransitionCode()
        : base(
            40000,
            "InvalidStateTransition",
            ResultSeverities.ByName("Error"),
            "Cannot transition from '{CurrentState}' to '{NewState}'",
            isRetryable: false)
    {
    }
}