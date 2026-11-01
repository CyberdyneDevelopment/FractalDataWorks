using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Results;

/// <summary>
/// Cast to expected service type failed.
/// </summary>
[TypeOption(typeof(ServicesResultCodes), "ServiceCastFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ServiceCastFailedCode : ServicesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceCastFailedCode"/> class.
    /// </summary>
    public ServiceCastFailedCode()
        : base(90002, "ServiceCastFailed",
            ResultSeverities.ByName("Error"),
            "Cast failed: expected '{ExpectedType}', actual '{ActualType}'",
            isRetryable: false)
    {
    }
}