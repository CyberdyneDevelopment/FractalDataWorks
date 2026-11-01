using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Mcp.Bus.Results;

/// <summary>
/// The bus subscription for {server}/{tool} closed before a matching response arrived (correlation {correlationId})
/// </summary>
[TypeOption(typeof(McpBusResultCodes), "SubscriptionClosedBeforeResponse", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SubscriptionClosedBeforeResponseCode : McpBusResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionClosedBeforeResponseCode"/> class.
    /// </summary>
    public SubscriptionClosedBeforeResponseCode()
        : base(91004, "SubscriptionClosedBeforeResponse", "BUS-91004", 91004,
            ResultSeverities.ByName("Error"),
            "The bus subscription for {server}/{tool} closed before a matching response arrived (correlation {correlationId})",
            isRetryable: false)
    {
    }
}
