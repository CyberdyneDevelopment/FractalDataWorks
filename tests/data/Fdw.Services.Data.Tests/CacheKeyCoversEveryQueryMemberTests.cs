using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Caching;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// FDW-3620: a cached read's key covers every member of a query that can change its answer. Two queries that differ only in a
/// projection, an aggregation, an Include flag or a Metadata entry a connection reads shared one entry, so one caller got the
/// other's rows. Every property of <see cref="IQueryCommand"/> must either change the key or be named here as left out, so a member
/// added later without a key entry fails.
/// </summary>
public sealed class CacheKeyCoversEveryQueryMemberTests
{
    private static readonly DataStoreTarget Target = new("PlatformConfiguration", "usr", "UsersConfiguration");

    // A query with every member set, so a pair can differ in exactly one.
    private static TestQuery Base() => new()
    {
        CommandId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
        CreatedAt = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
        CommandType = "Query",
        Category = "Data",
        Metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["GraphQLQuery"] = "query { users { id } }" },
        Filter = Where("Name", "Ada"),
        Ordering = new OrderingExpression { OrderedFields = [new OrderedField { PropertyName = "Name", Direction = new AscendingDirection() }] },
        Paging = new PagingExpression { Skip = 0, Take = 10 },
        Projection = new ProjectionExpression { Fields = [new ProjectionField { PropertyName = "Name" }] },
        Aggregation = new AggregationExpression { GroupByFields = ["Name"], Aggregations = new Dictionary<string, string> { ["Id"] = "Count" } },
        Joins = [new JoinExpression { TargetContainerName = "Users", JoinType = "Inner", JoinConditions = [("UsersRowId", "RowId")] }],
    };

    // For each member that changes the answer, the base query with only that member changed.
    private static readonly Dictionary<string, Func<TestQuery, TestQuery>> Changed = new(StringComparer.Ordinal)
    {
        [nameof(IQueryCommand.CommandType)] = query => query with { CommandType = "OtherQuery" },
        [nameof(IQueryCommand.Category)] = query => query with { Category = "Other" },
        [nameof(IQueryCommand.Metadata)] = query => query with
        {
            Metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["GraphQLQuery"] = "query { users { name } }" },
        },
        [nameof(IQueryCommand.Filter)] = query => query with { Filter = Where("Name", "Bob") },
        [nameof(IQueryCommand.Ordering)] = query => query with
        {
            Ordering = new OrderingExpression { OrderedFields = [new OrderedField { PropertyName = "Name", Direction = new DescendingDirection() }] },
        },
        [nameof(IQueryCommand.Paging)] = query => query with { Paging = new PagingExpression { Skip = 10, Take = 10 } },
        [nameof(IQueryCommand.Projection)] = query => query with
        {
            Projection = new ProjectionExpression { Fields = [new ProjectionField { PropertyName = "Email" }] },
        },
        [nameof(IQueryCommand.Aggregation)] = query => query with
        {
            Aggregation = new AggregationExpression
            {
                GroupByFields = ["Name"],
                Aggregations = new Dictionary<string, string> { ["Id"] = "Count" },
                IncludeGroupCount = true,
            },
        },
        [nameof(IQueryCommand.Joins)] = query => query with
        {
            Joins = [new JoinExpression { TargetContainerName = "Roles", JoinType = "Inner", JoinConditions = [("UsersRowId", "RowId")] }],
        },
        [nameof(IQueryCommand.ReadsForWrite)] = query => query with { ReadsForWrite = true },
        [nameof(IQueryCommand.IncludeMetadata)] = query => query with { IncludeMetadata = true },
        [nameof(IQueryCommand.IncludeDescriptions)] = query => query with { IncludeDescriptions = true },
        [nameof(IQueryCommand.IncludeKeys)] = query => query with { IncludeKeys = true },
    };

    // The members that identify one command instance and not the question it asks: keying them would make every read miss.
    private static readonly Dictionary<string, Func<TestQuery, TestQuery>> LeftOut = new(StringComparer.Ordinal)
    {
        [nameof(IQueryCommand.CommandId)] = query => query with { CommandId = Guid.Parse("00000000-0000-0000-0000-000000000002") },
        [nameof(IQueryCommand.CreatedAt)] = query => query with { CreatedAt = new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc) },
    };

    public static TheoryData<string> ChangedMembers() => [.. Changed.Keys];

    public static TheoryData<string> LeftOutMembers() => [.. LeftOut.Keys];

    private static FilterExpression Where(string property, object? value)
        => new() { Root = new FilterCondition { PropertyName = property, Operator = new EqualOperator(), Value = value } };

    private static string Key(IQueryCommand query) => CacheKeyBuilder.ComputeCacheKey(query, Target);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void EveryPropertyOfAQueryEitherChangesTheKeyOrIsNamedAsLeftOut()
    {
        var properties = new[] { typeof(IQueryCommand) }
            .Concat(typeof(IQueryCommand).GetInterfaces())
            .SelectMany(type => type.GetProperties())
            .Select(property => property.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        properties.ShouldBe(Changed.Keys.Concat(LeftOut.Keys).Order(StringComparer.Ordinal).ToList());
    }

    [Theory]
    [MemberData(nameof(ChangedMembers))]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TwoQueriesThatDifferOnlyInThisMemberGetDifferentKeys(string member)
        => Key(Changed[member](Base())).ShouldNotBe(Key(Base()));

    [Theory]
    [MemberData(nameof(LeftOutMembers))]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TwoQueriesThatDifferOnlyInThisMemberShareAKey(string member)
        => Key(LeftOut[member](Base())).ShouldBe(Key(Base()));

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualQueriesBuiltAsSeparateObjectsShareAKey()
        => Key(Base()).ShouldBe(Key(Base()));

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TheCachesOwnMetadataDoesNotChangeTheKey()
    {
        var steered = Base() with
        {
            Metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["GraphQLQuery"] = "query { users { id } }",
                ["CacheEnabled"] = true,
                ["CacheDuration"] = TimeSpan.FromMinutes(1),
            },
        };

        Key(steered).ShouldBe(Key(Base()));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValuesOfDifferentTypesWithTheSameTextGetDifferentKeys()
        => Key(Base() with { Filter = Where("Id", 1) }).ShouldNotBe(Key(Base() with { Filter = Where("Id", "1") }));

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TimesThatDifferBelowASecondGetDifferentKeys()
    {
        var at = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

        Key(Base() with { Filter = Where("ChangedAt", at) }).ShouldNotBe(Key(Base() with { Filter = Where("ChangedAt", at.AddMilliseconds(1)) }));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AFilterValueWithNoExactTextIsRefused()
        => Should.Throw<InvalidOperationException>(() => Key(Base() with { Filter = Where("Name", new object()) }));

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AFilterNodeThatIsNeitherAConditionNorAGroupIsRefused()
        => Should.Throw<InvalidOperationException>(() => Key(Base() with { Filter = new FilterExpression { Root = new OtherNode() } }));

    /// <summary>A filter node the key cannot describe.</summary>
    private sealed class OtherNode : IFilterNode
    {
    }

    /// <summary>A query whose every member can be set, so a pair can differ in exactly one.</summary>
    private sealed record TestQuery : IQueryCommand
    {
        public required Guid CommandId { get; init; }

        public required DateTime CreatedAt { get; init; }

        public required string CommandType { get; init; }

        public required string Category { get; init; }

        public required IReadOnlyDictionary<string, object> Metadata { get; init; }

        public IFilterExpression? Filter { get; init; }

        public IProjectionExpression? Projection { get; init; }

        public IOrderingExpression? Ordering { get; init; }

        public IPagingExpression? Paging { get; init; }

        public IAggregationExpression? Aggregation { get; init; }

        public required IReadOnlyList<IJoinExpression> Joins { get; init; }

        public bool ReadsForWrite { get; init; }

        public bool IncludeMetadata { get; init; }

        public bool IncludeDescriptions { get; init; }

        public bool IncludeKeys { get; init; }
    }
}
