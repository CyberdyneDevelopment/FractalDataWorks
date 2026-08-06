using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Results;

/// <summary>
/// No registration found for service type.
/// </summary>
[TypeOption(typeof(ServicesResultCodes), "RegistrationNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RegistrationNotFoundCode : ServicesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegistrationNotFoundCode"/> class.
    /// </summary>
    public RegistrationNotFoundCode()
        : base(61001, "RegistrationNotFound",
            ResultSeverities.ByName("Error"),
            "No registration found for service type '{TypeName}'. Available types: {AvailableTypes}",
            isRetryable: false)
    {
    }
}