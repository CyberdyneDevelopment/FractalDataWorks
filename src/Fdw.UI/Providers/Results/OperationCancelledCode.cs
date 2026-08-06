using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.UI.Providers.Results;

/// <summary>
/// A provider operation was cancelled before it completed.
/// </summary>
/// <remarks>
/// Why category 1 (non-error) and returned as a success: cancellation is the expected outcome when
/// a component is disposed or the user navigates away mid-request. Reporting it as a failure would
/// paint an error banner for a page that is already gone. Callers that need to distinguish it read
/// <c>Code</c>; callers that only branch on <c>IsSuccess</c> correctly do nothing.
/// </remarks>
[TypeOption(typeof(UIProviderResultCodes), "OperationCancelled", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OperationCancelledCode : UIProviderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationCancelledCode"/> class.
    /// </summary>
    public OperationCancelledCode()
        : base(11000, "OperationCancelled",
            ResultSeverities.ByName("Information"),
            "The operation was cancelled.",
            isRetryable: false)
    {
    }
}
