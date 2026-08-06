using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Mcp.Bus.Results;

/// <summary>
/// TypeCollection for MCP bus result codes.
/// Codes use the categorized-number scheme: Id == EventId == number, Code == "BUS-{number}".
/// </summary>
[TypeCollection(typeof(McpBusResultCodeBase), typeof(IResultCode), typeof(McpBusResultCodes))]
public abstract partial class McpBusResultCodes : TypeCollectionBase<McpBusResultCodeBase, IResultCode>
{
}
