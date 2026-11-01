using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Mcp.Bus.Tests;

public class InProcMcpToolSourceTests
{
    [Fact]
    public async Task SourceRespondsToInvokeWithResult()
    {
        var bus = new InMemoryMcpEventBus();
        await using var source = new InProcMcpToolSource("test",
            (toolName, args, ct) => Task.FromResult<object>(new { tool = toolName, echoed = args.GetProperty("value").GetInt32() }));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await source.Start(bus, cts.Token);
        await Task.Delay(50, cts.Token);

        var result = await McpRequestResponse.InvokeAndAwait(
            bus, "test", "echo", new { value = 42 }, ViewIntents.ByName("Update"),
            TimeSpan.FromSeconds(30), cancellationToken: cts.Token);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Topic.ShouldBe("mcp/test/echo/result");
        var payload = JsonDocument.Parse(System.Text.Encoding.UTF8.GetString(result.Value!.Payload.Span));
        payload.RootElement.GetProperty("echoed").GetInt32().ShouldBe(42);
    }

    [Fact]
    public async Task SourceHandlerThrowsPublishesErrorEvent()
    {
        var bus = new InMemoryMcpEventBus();
        await using var source = new InProcMcpToolSource("test",
            (toolName, args, ct) => throw new InvalidOperationException("deliberate"));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await source.Start(bus, cts.Token);
        await Task.Delay(50, cts.Token);

        var result = await McpRequestResponse.InvokeAndAwait(
            bus, "test", "boom", new { }, ViewIntents.ByName("Silent"),
            TimeSpan.FromSeconds(30), cancellationToken: cts.Token);

        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("ToolReportedError");
    }

    [Fact]
    public async Task SourceStopHaltsThePump()
    {
        var bus = new InMemoryMcpEventBus();
        var invocations = 0;
        var source = new InProcMcpToolSource("test",
            (t, a, ct) => { Interlocked.Increment(ref invocations); return Task.FromResult<object>(new { }); });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await source.Start(bus, cts.Token);
        await source.Stop(cts.Token);

        await bus.PublishToolInvocation("test", "after-stop", new { }, ViewIntents.ByName("Silent"), Guid.NewGuid(), cancellationToken: cts.Token);
        await Task.Delay(100, cts.Token);

        invocations.ShouldBe(0);
        await source.DisposeAsync();
    }
}
