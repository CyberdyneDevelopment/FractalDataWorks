using System;
using System.Collections.Generic;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Data.Caching;
using Microsoft.Extensions.Caching.Memory;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Tests;

/// <summary>
/// Covers the agreement the gateway's cache invalidation rests on: the tag a write derives from the
/// command it just ran must be the same string a later caller derives from an address alone, and a
/// write must never be answered from the cache.
/// </summary>
public sealed class GatewayCacheInvalidationTests
{
    private static DataStoreTarget Target => new("PlatformConfiguration", "conn", "Connection");

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Caching")]
    public void TagFromACommandAndTagFromAnAddressAreTheSameString()
    {
        CacheKeyBuilder.GetInvalidationTags(new InsertCommand<object>(new object()), Target)
            .ShouldHaveSingleItem()
            .ShouldBe(CacheKeyBuilder.TagFor(Target));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Caching")]
    public void TagIsSchemaDotTable()
    {
        CacheKeyBuilder.TagFor(Target).ShouldBe("conn.Connection");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Caching")]
    public void TwoDifferentWritesToOneContainerComputeTheSameCacheKey()
    {
        CacheKeyBuilder.ComputeCacheKey(new InsertCommand<object>(new object()), Target)
            .ShouldBe(CacheKeyBuilder.ComputeCacheKey(new InsertCommand<object>(new object()), Target));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Caching")]
    public void InvalidatingATagDropsEveryEntryStoredUnderIt()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new DataGatewayResultCache(memory, loggerFactory: null);
        var tag = CacheKeyBuilder.TagFor(Target);

        cache.Set("key-a", GenericResult<int>.Success(1), new[] { tag }, TimeSpan.FromMinutes(5));
        cache.Set("key-b", GenericResult<int>.Success(2), new[] { tag }, TimeSpan.FromMinutes(5));

        cache.InvalidateByTag(tag);

        cache.TryGet<int>("key-a", out _).ShouldBeFalse();
        cache.TryGet<int>("key-b", out _).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Caching")]
    public void InvalidatingOneTagLeavesAnotherContainerAlone()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new DataGatewayResultCache(memory, loggerFactory: null);
        var other = new DataStoreTarget("PlatformConfiguration", "sec", "SecretManager");

        cache.Set("conn-key", GenericResult<int>.Success(1),
            new[] { CacheKeyBuilder.TagFor(Target) }, TimeSpan.FromMinutes(5));
        cache.Set("sec-key", GenericResult<int>.Success(2),
            new[] { CacheKeyBuilder.TagFor(other) }, TimeSpan.FromMinutes(5));

        cache.InvalidateByTag(CacheKeyBuilder.TagFor(Target));

        cache.TryGet<int>("conn-key", out _).ShouldBeFalse();
        cache.TryGet<int>("sec-key", out var survivor).ShouldBeTrue();
        survivor!.Value.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Caching")]
    public void ACommandsOwnInvalidationTagsWinOverTheAddress()
    {
        var command = new InsertCommand<object>(new object())
        {
            Metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                [Fdw.Commands.Data.Abstractions.Caching.CachePolicy.CacheInvalidationTagsKey] =
                    new[] { "conn.Connection", "pipe.OrchestrationNode" },
            },
        };

        CacheKeyBuilder.GetInvalidationTags(command, Target)
            .ShouldBe(new[] { "conn.Connection", "pipe.OrchestrationNode" });
    }
}
