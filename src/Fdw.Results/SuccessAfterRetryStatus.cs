using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Success after retry - recovered via retry, prior exception info carried.
/// </summary>
[TypeOption(typeof(ResultStatuses), "SuccessAfterRetry", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SuccessAfterRetryStatus : ResultStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SuccessAfterRetryStatus"/> class.
    /// </summary>
    public SuccessAfterRetryStatus()
        : base(
            id: 2,
            name: "SuccessAfterRetry",
            isSuccess: true,
            requiresAttention: true)
    {
    }
}
