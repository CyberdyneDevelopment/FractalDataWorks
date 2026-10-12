using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Mcp.Bus.Results;

/// <summary>
/// {server}/{tool} reported an error: {error} (correlation {correlationId})
/// </summary>
[TypeOption(typeof(McpBusResultCodes), "ToolReportedError", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ToolReportedErrorCode : McpBusResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolReportedErrorCode"/> class.
    /// </summary>
    public ToolReportedErrorCode()
        : base(91005, "ToolReportedError", "BUS-91005", 91005,
            ResultSeverities.ByName("Error"),
            "{server}/{tool} reported an error: {error} (correlation {correlationId})",
            isRetryable: false)
    {
    }
}
