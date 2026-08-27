using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// No configuration gateway is registered for the requested connection.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "NoConfigurationGateway", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoConfigurationGatewayCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="NoConfigurationGatewayCode"/> class.</summary>
    public NoConfigurationGatewayCode()
        : base(61016, "NoConfigurationGateway", ResultSeverities.ByName("Error"),
            "No configuration gateway is registered for connection '{ConnectionName}'. Registered: {Registered}.",
            isRetryable: false)
    {
    }
}
