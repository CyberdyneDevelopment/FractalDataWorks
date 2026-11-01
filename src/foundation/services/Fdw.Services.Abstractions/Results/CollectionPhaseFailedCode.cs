using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.ServiceTypes.Results;

/// <summary>
/// A ServiceTypeCollection's registration phase did not complete.
/// </summary>
/// <remarks>
/// Carried when the collection's phase body threw, or when an option in its collect returned a failure
/// and the collect stopped there. Either way the domain is half-registered: the caller decides whether
/// that is fatal to the host, so this is returned rather than thrown.
/// </remarks>
[TypeOption(typeof(ServiceTypeResultCodes), "CollectionPhaseFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CollectionPhaseFailedCode : ServiceTypeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionPhaseFailedCode"/> class.
    /// </summary>
    public CollectionPhaseFailedCode()
        : base(61011, "CollectionPhaseFailed",
            ResultSeverities.ByName("Error"),
            "[{CollectionName}] {Phase} (collection #{Sequence}) FAILED while running the {Implementation} implementation: {ErrorMessage}",
            isRetryable: false)
    {
    }
}
