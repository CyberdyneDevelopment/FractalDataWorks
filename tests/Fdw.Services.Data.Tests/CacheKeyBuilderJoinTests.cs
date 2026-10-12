using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Caching;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A read's cache key names the question it asks (FDW-983). A child read is filtered on an owner's key and
/// joined to the owner, and to the domain row above it; the same filter joined differently is a different question.
/// </summary>
public sealed class CacheKeyBuilderJoinTests
{
    private static readonly DataStoreTarget Target = new("PlatformConfiguration", "usr", "UsersConfiguration");

    private static IDataCommand Child(string owner, string ownerColumn)
        => new QueryCommandBuilder<object>("PlatformConfiguration", "usr", "UsersConfiguration")
            .Join(owner, "UsersRowId", ownerColumn)
            .Where("IsCurrent", true)
            .Build().Command;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void TheSameFilterJoinedToDifferentContainersGivesDifferentKeys()
    {
        var one = CacheKeyBuilder.ComputeCacheKey(Child("Users", "RowId"), Target);
        var other = CacheKeyBuilder.ComputeCacheKey(Child("Roles", "RowId"), Target);

        one.ShouldNotBe(other);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void TheSameFilterJoinedOnDifferentColumnsGivesDifferentKeys()
    {
        var one = CacheKeyBuilder.ComputeCacheKey(Child("Users", "RowId"), Target);
        var other = CacheKeyBuilder.ComputeCacheKey(Child("Users", "OwnerRowId"), Target);

        one.ShouldNotBe(other);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void TheSameQueryBuiltTwiceGivesTheSameKey()
    {
        var one = CacheKeyBuilder.ComputeCacheKey(Child("Users", "RowId"), Target);
        var again = CacheKeyBuilder.ComputeCacheKey(Child("Users", "RowId"), Target);

        one.ShouldBe(again);
    }
}
