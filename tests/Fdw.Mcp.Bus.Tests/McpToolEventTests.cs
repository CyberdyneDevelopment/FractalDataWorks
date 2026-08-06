using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fdw.Mcp.Bus.Tests;

public class McpToolEventTests
{
    [Fact]
    public void McpTopicsBuildsConventionalNames()
    {
        McpTopics.ToolInvoke("mssql", "get_table_schema").ShouldBe("mcp/mssql/get_table_schema/invoke");
        McpTopics.ToolResult("mssql", "get_table_schema").ShouldBe("mcp/mssql/get_table_schema/result");
        McpTopics.ToolError("mssql", "get_table_schema").ShouldBe("mcp/mssql/get_table_schema/error");
        McpTopics.AnyToolResult().ShouldBe("mcp/*/*/result");
        McpTopics.AnyServerEvent("mssql").ShouldBe("mcp/mssql/**");
    }

    [Fact]
    public void McpTopicsRejectsSlashesInSegments()
    {
        Should.Throw<ArgumentException>(() => McpTopics.ToolInvoke("a/b", "tool"));
        Should.Throw<ArgumentException>(() => McpTopics.ToolResult("server", "a/b"));
    }

    [Fact]
    public async Task PublishToolInvocationAndResultCarryCorrelationAndCausation()
    {
        var bus = new InMemoryMcpEventBus();
        var update = ViewIntents.ByName("Update");
        var corr = Guid.NewGuid();

        var invokeId = await bus.PublishToolInvocation("mssql", "get_table_schema",
            new { table = "dbo.Orders" }, update, corr);
        await bus.PublishToolResult("mssql", "get_table_schema",
            new { columns = new[] { "Id", "Customer" } }, update, corr, causation: invokeId);

        var collected = new System.Collections.Generic.List<McpEvent>();
        await foreach (var evt in bus.Replay("mcp/mssql/get_table_schema/*", 1))
            collected.Add(evt);

        collected.Count.ShouldBe(2);
        collected[0].Topic.ShouldEndWith("/invoke");
        collected[1].Topic.ShouldEndWith("/result");
        collected[1].Causation.ShouldBe(invokeId);
        collected[0].CorrelationId.ShouldBe(corr);
        collected[1].CorrelationId.ShouldBe(corr);
    }

    [Fact]
    public async Task InvokeAndAwaitReturnsMatchingResult()
    {
        var bus = new InMemoryMcpEventBus();
        var silent = ViewIntents.ByName("Silent");

        // Why subscribe HERE and not inside the Task.Run: Subscribe registers eagerly and returns an
        // enumerable already wired to a live unbounded channel, so once this line has run the invoke
        // cannot be missed. Calling it inside the task instead races the publish against the thread
        // pool scheduling that task — lose the race and nothing is subscribed when the invoke is
        // published, the responder never fires, and InvokeAndAwait waits on a result that never comes.
        var invokes = bus.Subscribe("mcp/mssql/echo/invoke", TestContext.Current.CancellationToken);

        // Simulate the MCP server: when an invoke arrives, publish a result.
        _ = Task.Run(async () =>
        {
            await foreach (var evt in invokes)
            {
                await bus.PublishToolResult("mssql", "echo", new { echoed = true },
                    silent, evt.CorrelationId, causation: evt.EventId);
                break;
            }
        });

        var result = await McpRequestResponse.InvokeAndAwait(
            bus, "mssql", "echo", new { value = 42 }, silent, TimeSpan.FromSeconds(30),
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Topic.ShouldBe("mcp/mssql/echo/result");
        var doc = JsonDocument.Parse(Encoding.UTF8.GetString(result.Value!.Payload.Span));
        doc.RootElement.GetProperty("echoed").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAndAwaitReturnsFailureOnErrorEvent()
    {
        var bus = new InMemoryMcpEventBus();
        var silent = ViewIntents.ByName("Silent");

        // Subscribed here rather than inside the task, for the reason given above.
        var invokes = bus.Subscribe("mcp/mssql/boom/invoke", TestContext.Current.CancellationToken);

        _ = Task.Run(async () =>
        {
            await foreach (var evt in invokes)
            {
                await bus.PublishToolError("mssql", "boom", "deliberate failure",
                    evt.CorrelationId, causation: evt.EventId);
                break;
            }
        });

        var result = await McpRequestResponse.InvokeAndAwait(
            bus, "mssql", "boom", new { }, silent, TimeSpan.FromSeconds(30),
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("ToolReportedError");
    }
}
