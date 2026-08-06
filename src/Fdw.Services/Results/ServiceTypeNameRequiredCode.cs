using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Results;

/// <summary>
/// Service type name was null or empty.
/// </summary>
[TypeOption(typeof(ServicesResultCodes), "ServiceTypeNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ServiceTypeNameRequiredCode : ServicesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceTypeNameRequiredCode"/> class.
    /// </summary>
    public ServiceTypeNameRequiredCode()
        : base(20000, "ServiceTypeNameRequired",
            ResultSeverities.ByName("Error"),
            "Service type name cannot be null or empty",
            isRetryable: false)
    {
    }
}