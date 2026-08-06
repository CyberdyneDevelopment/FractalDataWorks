namespace Fdw.Mcp.Bus.Tests;

public class McpToolSourceTypesTests
{
    [Fact]
    public void InProcIsRegistered()
    {
        var kind = McpToolSourceTypes.ByName("InProc");
        kind.ShouldNotBe(McpToolSourceTypes.NotFound);
        kind.Name.ShouldBe("InProc");
    }

    [Fact]
    public void StdioBridgeIsRegistered()
    {
        var kind = McpToolSourceTypes.ByName("StdioBridge");
        kind.ShouldNotBe(McpToolSourceTypes.NotFound);
        kind.Name.ShouldBe("StdioBridge");
    }

    [Fact]
    public void UnknownKindReturnsNotFound()
    {
        McpToolSourceTypes.ByName("DefinitelyNot").ShouldBe(McpToolSourceTypes.NotFound);
    }
}
