using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A configuration write in one host is dropped from another host's cache at that host's next look (FDW-4782): the
/// writing host publishes a token per tag it changed, and the other host's poller drops every tag whose token moved.
/// Two hosts here are two caches, a publisher and a poller over one token table, as two processes are over one database.
/// </summary>
public sealed class CacheChangeAcrossHostsTests
{
    private const string Tag = "conn.Connection";
    private const string Key = "partition|_cfg|conn.Connection:f=0000abcd:System.String";

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AWriteInOneHostIsSeenByTheNextReadInAnother()
    {
        var table = new TokenTable();
        var writer = Publisher(table);
        var readerCache = Cache();
        var reader = Poller(table, readerCache);

        // The reading host has the old value cached, and has taken its first look.
        readerCache.Set(Key, GenericResult<string>.Success("before the write"), [Tag], TimeSpan.FromMinutes(5));
        (await reader.Observe(TestContext.Current.CancellationToken)).ShouldBeEmpty();
        readerCache.TryGet<string>(Key, out _).ShouldBeTrue("nothing has changed yet");

        // The writing host changes the configuration and publishes it.
        writer.Publish([Tag]);
        await writer.FlushPending(TestContext.Current.CancellationToken);
        table.Rows.Keys.ShouldBe([Tag]);

        // The reading host's next look drops it, so its next read goes to the database.
        (await reader.Observe(TestContext.Current.CancellationToken)).ShouldBe([Tag]);
        readerCache.TryGet<string>(Key, out _).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASecondWriteToTheSameTagIsSeenToo()
    {
        var table = new TokenTable();
        var writer = Publisher(table);
        var readerCache = Cache();
        var reader = Poller(table, readerCache);

        writer.Publish([Tag]);
        await writer.FlushPending(TestContext.Current.CancellationToken);
        (await reader.Observe(TestContext.Current.CancellationToken)).ShouldBeEmpty("the first look only records");

        readerCache.Set(Key, GenericResult<string>.Success("after the first write"), [Tag], TimeSpan.FromMinutes(5));
        writer.Publish([Tag]);
        await writer.FlushPending(TestContext.Current.CancellationToken);

        (await reader.Observe(TestContext.Current.CancellationToken)).ShouldBe([Tag]);
        readerCache.TryGet<string>(Key, out _).ShouldBeFalse();
        table.Inserts.ShouldBe(1, "the second write updates the row the first inserted");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task TheFirstLookOnlyRecordsAndDropsNothing()
    {
        var table = new TokenTable();
        var writer = Publisher(table);
        writer.Publish([Tag]);
        await writer.FlushPending(TestContext.Current.CancellationToken);

        var readerCache = Cache();
        readerCache.Set(Key, GenericResult<string>.Success("cached"), [Tag], TimeSpan.FromMinutes(5));
        var reader = Poller(table, readerCache);

        (await reader.Observe(TestContext.Current.CancellationToken)).ShouldBeEmpty();
        readerCache.TryGet<string>(Key, out _).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task WritesQueuedTogetherArePublishedOnceEach()
    {
        var table = new TokenTable();
        var writer = Publisher(table);

        writer.Publish([Tag]);
        writer.Publish([Tag, "conn.Secret"]);
        writer.Publish([Tag]);

        // The background loop may have flushed some already; when this returns, all of them are out.
        await writer.FlushPending(TestContext.Current.CancellationToken);

        table.Rows.Keys.OrderBy(tag => tag, StringComparer.Ordinal).ShouldBe(["conn.Connection", "conn.Secret"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task TheTokenContainersOwnTagIsNeverPublished()
    {
        var table = new TokenTable();
        var writer = Publisher(table);

        writer.Publish([CacheKeyBuilder.TagFor(CacheChangeTokens.Target)]);
        await writer.FlushPending(TestContext.Current.CancellationToken);

        table.Rows.ShouldBeEmpty();
        table.Updates.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnInsertThatLosesTheRaceToAnotherHostIsRetriedAsAnUpdate()
    {
        var table = new TokenTable();
        var writer = Publisher(table);

        // Another host inserts the tag between this host's update (which matched nothing) and its insert.
        table.BeforeInsert = () => table.Rows[Tag] = new CacheChangeToken { Tag = Tag, Token = Guid.CreateVersion7(), ChangedAt = DateTimeOffset.UtcNow };

        writer.Publish([Tag]);
        await writer.FlushPending(TestContext.Current.CancellationToken);

        table.Updates.ShouldBe(2);
        table.Inserts.ShouldBe(0, "the other host's insert won");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ATokenThatCannotBeWrittenIsLoggedAndDroppedWithoutFailingThePublish()
    {
        var table = new TokenTable { FailUpdates = true };
        var writer = Publisher(table);

        writer.Publish([Tag]);
        await writer.FlushPending(TestContext.Current.CancellationToken);

        table.Rows.ShouldBeEmpty();
        table.Inserts.ShouldBe(0, "an update that fails is not taken for a missing row");

        // The next write is published once the table takes writes again: nothing was left half queued.
        table.FailUpdates = false;
        writer.Publish([Tag]);
        await writer.FlushPending(TestContext.Current.CancellationToken);
        table.Rows.Keys.ShouldBe([Tag]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFlushTheCallerCancelsDropsNoQueuedChange()
    {
        var table = new TokenTable();
        var writer = Publisher(table);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        writer.Publish([Tag, "conn.Secret"]);

        // The background loop may have published them already; if this caller's flush took them, it puts them back.
        try
        {
            _ = await writer.FlushPending(cancelled.Token);
        }
        catch (OperationCanceledException)
        {
        }

        await writer.FlushPending(TestContext.Current.CancellationToken);

        table.Rows.Keys.OrderBy(tag => tag, StringComparer.Ordinal).ShouldBe(["conn.Connection", "conn.Secret"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ATokenReadThatFailsDropsNothingAndTheNextLookStillSeesTheChange()
    {
        var table = new TokenTable();
        var writer = Publisher(table);
        var readerCache = Cache();
        var reader = Poller(table, readerCache);
        await reader.Observe(TestContext.Current.CancellationToken);

        readerCache.Set(Key, GenericResult<string>.Success("before the write"), [Tag], TimeSpan.FromMinutes(5));
        writer.Publish([Tag]);
        await writer.FlushPending(TestContext.Current.CancellationToken);

        table.FailReads = true;
        (await reader.Observe(TestContext.Current.CancellationToken)).ShouldBeEmpty();
        readerCache.TryGet<string>(Key, out _).ShouldBeTrue();

        table.FailReads = false;
        (await reader.Observe(TestContext.Current.CancellationToken)).ShouldBe([Tag]);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void AHostWithACacheStatesAnInterval()
        => Should.Throw<ArgumentNullException>(() => new CacheChangePoller(
            () => GenericResult<IConfigurationGateway>.Success(new TokenTableGateway(new TokenTable())),
            new Accessor(),
            Cache(),
            CacheChangeTokens.Target,
            null));

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void AnIntervalThatIsNotPositiveIsRefused(int seconds)
        => Should.Throw<ArgumentOutOfRangeException>(() => new CacheChangePoller(
            () => GenericResult<IConfigurationGateway>.Success(new TokenTableGateway(new TokenTable())),
            new Accessor(),
            Cache(),
            CacheChangeTokens.Target,
            TimeSpan.FromSeconds(seconds)));

    private static DataGatewayResultCache Cache()
        => new(new MemoryCache(new MemoryCacheOptions()), NullLoggerFactory.Instance);

    private static CacheChangePublisher Publisher(TokenTable table)
        => new(() => GenericResult<IConfigurationGateway>.Success(new TokenTableGateway(table)), new Accessor(), CacheChangeTokens.Target);

    private static CacheChangePoller Poller(TokenTable table, DataGatewayResultCache cache)
        => new(() => GenericResult<IConfigurationGateway>.Success(new TokenTableGateway(table)), new Accessor(), cache, CacheChangeTokens.Target, TimeSpan.FromSeconds(10));

    private sealed class Accessor : IAuthenticationContextAccessor
    {
        public IAuthenticationContext? Current { get; set; }
    }

    // The token table the hosts share, as two processes share one database.
    private sealed class TokenTable
    {
        public ConcurrentDictionary<string, CacheChangeToken> Rows { get; } = new(StringComparer.OrdinalIgnoreCase);

        public int Inserts { get; set; }

        public int Updates { get; set; }

        public bool FailUpdates { get; set; }

        public bool FailReads { get; set; }

        public Action? BeforeInsert { get; set; }
    }

    // A gateway over the token table: Update by tag, Insert (failing on a duplicate tag), and Query of every row.
    private sealed class TokenTableGateway(TokenTable table) : IConfigurationGateway
    {
        public string ConnectionName => "PlatformConfiguration";

        public IReadOnlyList<IDataStore> DataStores => [];

        string IPlatformService.Id => "TokenTableGateway";

        string IPlatformService.ServiceType => "DataGateway";

        bool IPlatformService.IsAvailable => true;

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => Execute<T>(command, target, useCache: true, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
        {
            switch (command)
            {
                case UpdateCommand<CacheChangeToken> update:
                    table.Updates++;
                    if (table.FailUpdates)
                        return Failed<T>("the update failed");

                    if (!table.Rows.ContainsKey(update.Data.Tag))
                        return Answer<T>(0);

                    table.Rows[update.Data.Tag] = update.Data;
                    return Answer<T>(1);

                case InsertCommand<CacheChangeToken> insert:
                    table.BeforeInsert?.Invoke();
                    var row = insert.Data.Single();
                    if (!table.Rows.TryAdd(row.Tag, row))
                        return Failed<T>("duplicate key");

                    table.Inserts++;
                    return Answer<T>(1);

                case QueryCommand<CacheChangeToken>:
                    return table.FailReads
                        ? Failed<T>("the read failed")
                        : Answer<T>(table.Rows.Values.Select(r => new CacheChangeToken { Tag = r.Tag, Token = r.Token, ChangedAt = r.ChangedAt }).ToList());

                default:
                    return Failed<T>("not a token command");
            }
        }

        public void InvalidateCachedResults(DataStoreTarget target)
        {
        }

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default)
            => Failed<T>("not used");

        public Task<IGenericResult<IRecordSource<DataRecord>>> OpenRecordSource(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => Failed<IRecordSource<DataRecord>>("not used");


        public Task<IGenericResult<IRecordSource<DataRecord>>> OpenRecordSource(IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default)
            => Failed<IRecordSource<DataRecord>>("not used");

        public Task<IGenericResult<T>> Execute<T>(IVectorizedDataCommand<T> command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => Failed<T>("not used");

        public Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(string connectionName, CancellationToken cancellationToken = default)
            => Failed<IDataGatewayTransaction>("not used");

        public Task<IGenericResult> Execute(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult>(GenericResult.Failure(new GenericMessage("not used")));

        public Task<IGenericResult<IEnumerable<object>>> Execute(IDataCommand command, DataStoreTarget target, Type rowType, CancellationToken cancellationToken = default)
            => Failed<IEnumerable<object>>("not used");

        Task<IGenericResult<T>> IGenericService.Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
            => Failed<T>("not used");

        Task<IGenericResult> IGenericService.Execute(IGenericCommand command, CancellationToken cancellationToken)
            => Task.FromResult<IGenericResult>(GenericResult.Failure(new GenericMessage("not used")));

        private static Task<IGenericResult<T>> Answer<T>(object value)
            => Task.FromResult<IGenericResult<T>>(GenericResult<T>.Success((T)value));

        private static Task<IGenericResult<T>> Failed<T>(string reason)
            => Task.FromResult<IGenericResult<T>>(GenericResult<T>.Failure(new GenericMessage(reason)));
    }
}
