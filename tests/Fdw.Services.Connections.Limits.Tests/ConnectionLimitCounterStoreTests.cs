using System;
using System.Threading.Tasks;
using Fdw.Services.Data.Limits;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Limits.Tests;

public sealed class ConnectionLimitCounterStoreTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Limits")]
    public void IncrementQueryCount_FirstCall_ReturnsOne()
    {
        var store = new ConnectionLimitCounterStore();
        var id = Guid.NewGuid();

        store.IncrementQueryCount(id).ShouldBe(1L);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Limits")]
    public void IncrementQueryCount_MultipleCalls_Accumulates()
    {
        var store = new ConnectionLimitCounterStore();
        var id = Guid.NewGuid();

        store.IncrementQueryCount(id);
        store.IncrementQueryCount(id);
        store.IncrementQueryCount(id);

        store.ReadQueryCount(id).ShouldBe(3L);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Limits")]
    public void ReadQueryCount_UnknownId_ReturnsZero()
    {
        var store = new ConnectionLimitCounterStore();

        store.ReadQueryCount(Guid.NewGuid()).ShouldBe(0L);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Limits")]
    public void Seed_SetsInitialCounter()
    {
        var store = new ConnectionLimitCounterStore();
        var id = Guid.NewGuid();
        var lastReset = DateTimeOffset.UtcNow.AddHours(-1);

        store.Seed(id, 500L, lastReset);

        store.ReadQueryCount(id).ShouldBe(500L);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Limits")]
    public void ResetAll_ZeroesAllCounters()
    {
        var store = new ConnectionLimitCounterStore();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        store.IncrementQueryCount(id1);
        store.IncrementQueryCount(id1);
        store.IncrementQueryCount(id2);

        store.ResetAll();

        store.ReadQueryCount(id1).ShouldBe(0L);
        store.ReadQueryCount(id2).ShouldBe(0L);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Limits")]
    public void Seed_DoesNotOverwriteExistingEntry()
    {
        var store = new ConnectionLimitCounterStore();
        var id = Guid.NewGuid();

        // First, increment the counter
        store.IncrementQueryCount(id);

        // Now try to seed — it should NOT overwrite the existing entry (TryAdd semantics)
        store.Seed(id, 999L, DateTimeOffset.UtcNow);

        // Should still be 1, not 999
        store.ReadQueryCount(id).ShouldBe(1L);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Limits")]
    public async Task IncrementQueryCount_ConcurrentIncrements_AreThreadSafe()
    {
        var store = new ConnectionLimitCounterStore();
        var id = Guid.NewGuid();
        const int parallelism = 100;

        var tasks = new Task[parallelism];
        for (int i = 0; i < parallelism; i++)
        {
            tasks[i] = Task.Run(() => store.IncrementQueryCount(id));
        }

        await Task.WhenAll(tasks);

        store.ReadQueryCount(id).ShouldBe(parallelism);
    }
}
