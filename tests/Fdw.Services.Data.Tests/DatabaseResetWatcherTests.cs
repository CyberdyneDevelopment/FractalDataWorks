using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A host empties its result cache when a configuration database is dropped and created again (FDW-3207), so a read made
/// after the reset returns the new rows and not the ones cached before it. What is asserted is what a read returns after
/// the watcher has looked, and how many times the cache was emptied.
/// </summary>
public sealed class DatabaseResetWatcherTests
{
    private const string Tag = "conn.Connection";

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AReadAfterAResetReturnsTheNewRowsNotTheCachedOnes()
    {
        var cache = new DataGatewayResultCache(new MemoryCache(new MemoryCacheOptions()), NullLoggerFactory.Instance);
        var database = new FakeDatabase("1@2026-10-29T10:00:00Z", "admin-before-reset");
        var gateway = new CachingGateway("ConfigurationDb", database, cache);
        var watcher = Watcher(cache, gateway);
        await watcher.Observe(TestContext.Current.CancellationToken);

        (await gateway.Read(TestContext.Current.CancellationToken)).ShouldBe("admin-before-reset");

        database.Reset("1@2026-10-29T10:05:00Z", "admin-after-reset");
        (await gateway.Read(TestContext.Current.CancellationToken)).ShouldBe("admin-before-reset", "the cache serves the old row until the watcher looks");

        (await watcher.Observe(TestContext.Current.CancellationToken)).ShouldBeTrue();

        (await gateway.Read(TestContext.Current.CancellationToken)).ShouldBe("admin-after-reset");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task TheFirstLookOnlyRecordsAndEmptiesNothing()
    {
        var invalidator = new CountingInvalidator();
        var watcher = new DatabaseResetWatcher(
            new FakeCatalog(new CachingGateway("ConfigurationDb", new FakeDatabase("g1", "row"), Cache())),
            invalidator,
            TimeSpan.FromSeconds(10));

        (await watcher.Observe(TestContext.Current.CancellationToken)).ShouldBeFalse();

        invalidator.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task AnUnchangedGenerationEmptiesNothingOnLaterLooks()
    {
        var invalidator = new CountingInvalidator();
        var watcher = new DatabaseResetWatcher(
            new FakeCatalog(new CachingGateway("ConfigurationDb", new FakeDatabase("g1", "row"), Cache())),
            invalidator,
            TimeSpan.FromSeconds(10));

        await watcher.Observe(TestContext.Current.CancellationToken);
        await watcher.Observe(TestContext.Current.CancellationToken);
        await watcher.Observe(TestContext.Current.CancellationToken);

        invalidator.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task AChangedGenerationEmptiesTheCacheOnceAndNotAgainWhileItStaysTheSame()
    {
        var invalidator = new CountingInvalidator();
        var database = new FakeDatabase("g1", "row");
        var watcher = new DatabaseResetWatcher(
            new FakeCatalog(new CachingGateway("ConfigurationDb", database, Cache())),
            invalidator,
            TimeSpan.FromSeconds(10));
        await watcher.Observe(TestContext.Current.CancellationToken);

        database.Reset("g2", "row");
        await watcher.Observe(TestContext.Current.CancellationToken);
        await watcher.Observe(TestContext.Current.CancellationToken);

        invalidator.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ADatabaseThatCannotBeReachedEmptiesTheCacheAndSoDoesItsReturn()
    {
        var invalidator = new CountingInvalidator();
        var database = new FakeDatabase("g1", "row");
        var watcher = new DatabaseResetWatcher(
            new FakeCatalog(new CachingGateway("ConfigurationDb", database, Cache())),
            invalidator,
            TimeSpan.FromSeconds(10));
        await watcher.Observe(TestContext.Current.CancellationToken);

        database.GoDown();
        await watcher.Observe(TestContext.Current.CancellationToken);
        invalidator.Count.ShouldBe(1);

        await watcher.Observe(TestContext.Current.CancellationToken);
        invalidator.Count.ShouldBe(1, "still down, nothing changed since the last look");

        database.Reset("g2", "row");
        await watcher.Observe(TestContext.Current.CancellationToken);
        invalidator.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ADatabaseUnreachableAtTheFirstLookIsRecordedAndEmptiesNothing()
    {
        var invalidator = new CountingInvalidator();
        var database = new FakeDatabase("g1", "row");
        database.GoDown();
        var watcher = new DatabaseResetWatcher(
            new FakeCatalog(new CachingGateway("ConfigurationDb", database, Cache())),
            invalidator,
            TimeSpan.FromSeconds(10));

        await watcher.Observe(TestContext.Current.CancellationToken);
        invalidator.Count.ShouldBe(0);

        database.Reset("g1", "row");
        await watcher.Observe(TestContext.Current.CancellationToken);
        invalidator.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task AGatewayWhoseConnectionCannotTellIsLeftAloneAndEmptiesNothing()
    {
        var invalidator = new CountingInvalidator();
        var watcher = new DatabaseResetWatcher(new FakeCatalog(new CannotTellGateway("ServerConfiguration")), invalidator, TimeSpan.FromSeconds(10));

        await watcher.Observe(TestContext.Current.CancellationToken);
        await watcher.Observe(TestContext.Current.CancellationToken);

        invalidator.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task AGatewayThatThrowsIsAnUnreachableDatabaseAndTheOthersAreStillLookedAt()
    {
        var invalidator = new CountingInvalidator();
        var broken = new CachingGateway("AuthDb", new FakeDatabase("a1", "row"), Cache());
        var steady = new FakeDatabase("c1", "row");
        var watcher = new DatabaseResetWatcher(
            new FakeCatalog(broken, new CachingGateway("ConfigurationDb", steady, Cache())),
            invalidator,
            TimeSpan.FromSeconds(10));
        await watcher.Observe(TestContext.Current.CancellationToken);

        broken.ThrowOnAsk = true;
        steady.Reset("c2", "row");
        await watcher.Observe(TestContext.Current.CancellationToken);

        invalidator.Count.ShouldBe(1, "both changes were found in one look, and the cache is emptied once for the look");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ADatabaseResetIsFoundWhicheverGatewayItIsReachedThrough()
    {
        var invalidator = new CountingInvalidator();
        var auth = new FakeDatabase("a1", "row");
        var watcher = new DatabaseResetWatcher(
            new FakeCatalog(
                new CachingGateway("ConfigurationDb", new FakeDatabase("c1", "row"), Cache()),
                new CachingGateway("AuthDb", auth, Cache())),
            invalidator,
            TimeSpan.FromSeconds(10));
        await watcher.Observe(TestContext.Current.CancellationToken);

        auth.Reset("a2", "row");

        (await watcher.Observe(TestContext.Current.CancellationToken)).ShouldBeTrue();
        invalidator.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void AnIntervalThatIsNotPositiveIsRefused(int seconds)
        => Should.Throw<ArgumentOutOfRangeException>(
            () => new DatabaseResetWatcher(new FakeCatalog(), new CountingInvalidator(), TimeSpan.FromSeconds(seconds)));

    private static DataGatewayResultCache Cache()
        => new(new MemoryCache(new MemoryCacheOptions()), NullLoggerFactory.Instance);

    private static DatabaseResetWatcher Watcher(DataGatewayResultCache cache, params IDatabaseGenerationSource[] sources)
        => new(new FakeCatalog(sources), cache, TimeSpan.FromSeconds(10));

    private sealed class CountingInvalidator : ICacheInvalidator
    {
        public int Count { get; private set; }

        public void InvalidateByTag(string tag)
        {
        }

        public void InvalidateByTags(IEnumerable<string> tags)
        {
        }

        public void InvalidateAll() => Count++;
    }

    private sealed class FakeCatalog(params IDatabaseGenerationSource[] sources) : IDatabaseGenerationCatalog
    {
        public IReadOnlyList<IDatabaseGenerationSource> Sources() => sources;
    }

    // The database a gateway reads: its generation, the row it holds, and whether it can be reached.
    private sealed class FakeDatabase(string generation, string row)
    {
        public string Generation { get; private set; } = generation;

        public string Row { get; private set; } = row;

        public bool Down { get; private set; }

        public void Reset(string generation, string row)
        {
            Generation = generation;
            Row = row;
            Down = false;
        }

        public void GoDown() => Down = true;
    }

    // A gateway that reads its one row through the shared cache, as ConfigurationGateway does, and tells its database's generation.
    private sealed class CachingGateway(string connectionName, FakeDatabase database, DataGatewayResultCache cache)
        : IDatabaseGenerationSource
    {
        public string ConnectionName => connectionName;

        public bool ThrowOnAsk { get; set; }

        public Task<string> Read(CancellationToken cancellationToken)
        {
            if (cache.TryGet<string>(connectionName, out var cached) && cached is { IsSuccess: true, Value: { } hit })
                return Task.FromResult(hit);

            cache.Set(connectionName, GenericResult<string>.Success(database.Row), [Tag], TimeSpan.FromMinutes(5));
            return Task.FromResult(database.Row);
        }

        public Task<IGenericResult<DatabaseGenerationReading>> DatabaseGeneration(CancellationToken cancellationToken = default)
        {
            if (ThrowOnAsk)
                throw new InvalidOperationException("the connection threw");

            return Task.FromResult<IGenericResult<DatabaseGenerationReading>>(database.Down
                ? GenericResult<DatabaseGenerationReading>.Failure(new GenericMessage("the database is not there"))
                : GenericResult<DatabaseGenerationReading>.Success(DatabaseGenerationReading.Of(database.Generation)));
        }
    }

    private sealed class CannotTellGateway(string connectionName) : IDatabaseGenerationSource
    {
        public string ConnectionName => connectionName;

        public Task<IGenericResult<DatabaseGenerationReading>> DatabaseGeneration(CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult<DatabaseGenerationReading>>(
                GenericResult<DatabaseGenerationReading>.Success(DatabaseGenerationReading.CannotTell));
    }
}
