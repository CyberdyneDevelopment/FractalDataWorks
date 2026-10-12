using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Results;

/// <summary>
/// Factory already registered for service type.
/// </summary>
[TypeOption(typeof(ServicesResultCodes), "FactoryAlreadyRegistered", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FactoryAlreadyRegisteredCode : ServicesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FactoryAlreadyRegisteredCode"/> class.
    /// </summary>
    public FactoryAlreadyRegisteredCode()
        : base(40001, "FactoryAlreadyRegistered",
            ResultSeverities.ByName("Error"),
            "Factory for service type '{TypeName}' is already registered",
            isRetryable: false)
    {
    }
}