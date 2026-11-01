using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Mcp.Bus.Tests;

public class InMemoryMcpEventBusTests
{
    private static McpEventDraft Draft(string topic, ulong? causation = null) =>
        new(
            Topic: topic,
            CorrelationId: Guid.NewGuid(),
            Causation: causation,
            View: ViewIntents.ByName("Update"),
            PayloadType: "test",
            Payload: ReadOnlyMemory<byte>.Empty);

    [Fact]
    public async Task PublishAssignsMonotonicEventIds()
    {
        var bus = new InMemoryMcpEventBus();
        var id1 = await bus.Publish(Draft("a/1"));
        var id2 = await bus.Publish(Draft("a/2"));
        var id3 = await bus.Publish(Draft("a/3"));

        id1.ShouldBe(1ul);
        id2.ShouldBe(2ul);
        id3.ShouldBe(3ul);
    }

    [Fact]
    public async Task SubscribeDeliversEventsMatchingPattern()
    {
        var bus = new InMemoryMcpEventBus();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var received = new List<McpEvent>();

        var pump = Task.Run(async () =>
        {
            await foreach (var evt in bus.Subscribe("mssql/**", cts.Token).ConfigureAwait(false))
            {
                received.Add(evt);
                if (received.Count == 2) break;
            }
        });

        await Task.Delay(50);
        await bus.Publish(Draft("mssql/103/schema"));
        await bus.Publish(Draft("roslyn/symbol/Foo"));
        await bus.Publish(Draft("mssql/105/schema"));

        await pump;

        received.Count.ShouldBe(2);
        received[0].Topic.ShouldBe("mssql/103/schema");
        received[1].Topic.ShouldBe("mssql/105/schema");
    }

    [Fact]
    public async Task ReplayReturnsEventsFromGivenIdMatchingPattern()
    {
        var bus = new InMemoryMcpEventBus();
        await bus.Publish(Draft("a/1"));
        await bus.Publish(Draft("a/2"));
        await bus.Publish(Draft("b/3"));
        await bus.Publish(Draft("a/4"));

        var replayed = new List<McpEvent>();
        await foreach (var evt in bus.Replay("a/*", fromEventId: 2))
            replayed.Add(evt);

        replayed.Select(e => e.Topic).ShouldBe(new[] { "a/2", "a/4" });
    }

    [Fact]
    public async Task ReplayCausationFollowsTheChain()
    {
        var bus = new InMemoryMcpEventBus();
        var root = await bus.Publish(Draft("root"));
        var child = await bus.Publish(Draft("child", causation: root));
        await bus.Publish(Draft("unrelated"));
        var grandchild = await bus.Publish(Draft("grandchild", causation: child));

        var chain = new List<ulong>();
        await foreach (var evt in bus.ReplayCausation(root))
            chain.Add(evt.EventId);

        chain.ShouldBe(new[] { root, child, grandchild });
    }

    [Fact]
    public async Task RingDropsOldestWhenCapacityExceeded()
    {
        var bus = new InMemoryMcpEventBus(ringCapacity: 3);
        await bus.Publish(Draft("a/1"));
        await bus.Publish(Draft("a/2"));
        await bus.Publish(Draft("a/3"));
        await bus.Publish(Draft("a/4"));

        var replayed = new List<ulong>();
        await foreach (var evt in bus.Replay("**", fromEventId: 1))
            replayed.Add(evt.EventId);

        replayed.ShouldBe(new[] { 2ul, 3ul, 4ul });
    }
}
