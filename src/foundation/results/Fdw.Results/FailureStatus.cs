using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Failure status - operation failed.
/// </summary>
[TypeOption(typeof(ResultStatuses), "Failure", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FailureStatus : ResultStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailureStatus"/> class.
    /// </summary>
    public FailureStatus()
        : base(
            id: 4,
            name: "Failure",
            isSuccess: false,
            requiresAttention: true)
    {
    }
}
