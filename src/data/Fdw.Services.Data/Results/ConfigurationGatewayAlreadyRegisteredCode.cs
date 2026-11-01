using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A configuration gateway is already registered for the requested connection.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "ConfigurationGatewayAlreadyRegistered", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ConfigurationGatewayAlreadyRegisteredCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ConfigurationGatewayAlreadyRegisteredCode"/> class.</summary>
    public ConfigurationGatewayAlreadyRegisteredCode()
        : base(61017, "ConfigurationGatewayAlreadyRegistered", ResultSeverities.ByName("Error"),
            "A configuration gateway is already registered for connection '{ConnectionName}'.",
            isRetryable: false)
    {
    }
}
