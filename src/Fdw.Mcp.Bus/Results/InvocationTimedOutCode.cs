using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Mcp.Bus.Results;

/// <summary>
/// {server}/{tool} did not answer within {timeout} (correlation {correlationId})
/// </summary>
[TypeOption(typeof(McpBusResultCodes), "InvocationTimedOut", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvocationTimedOutCode : McpBusResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvocationTimedOutCode"/> class.
    /// </summary>
    public InvocationTimedOutCode()
        : base(81000, "InvocationTimedOut", "BUS-81000", 81000,
            ResultSeverities.ByName("Error"),
            "{server}/{tool} did not answer within {timeout} (correlation {correlationId})",
            isRetryable: true)
    {
    }
}
