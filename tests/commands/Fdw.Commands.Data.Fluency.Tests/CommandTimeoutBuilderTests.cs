using System;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions.Timeouts;
using Fdw.Commands.Data.Extensions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Extensions.Tests;

/// <summary>
/// A query or an update built with a command timeout carries it, with its owner, on the command it builds (FDW-3324).
/// </summary>
public sealed class CommandTimeoutBuilderTests
{
    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AQueryBuiltWithACommandTimeoutCarriesItAndItsOwner()
    {
        var call = new QueryCommandBuilder<TestEntity>("TestDb", "dbo", "Secret")
            .Where("Id", 1)
            .CommandTimeout(TimeSpan.FromSeconds(2), "secret manager 'MsSqlSecrets'")
            .Build();

        CommandTimeoutPolicy.TryGet(call.Command, out var timeout, out var owner).ShouldBeTrue();
        timeout.ShouldBe(TimeSpan.FromSeconds(2));
        owner.ShouldBe("secret manager 'MsSqlSecrets'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AQueryBuiltWithoutOneCarriesNone()
    {
        var call = new QueryCommandBuilder<TestEntity>("TestDb", "dbo", "Secret").Where("Id", 1).Build();

        CommandTimeoutPolicy.TryGet(call.Command, out _, out _).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnUpdateBuiltWithACommandTimeoutCarriesItAndItsOwner()
    {
        var call = Update.In<TestEntity>("Secret")
            .DataStore("TestDb")
            .Path("dbo")
            .Where("Id", 1)
            .CommandTimeout(TimeSpan.FromSeconds(3), "secret manager 'MsSqlSecrets'")
            .Value(new TestEntity { Id = 1, Name = "x" });

        CommandTimeoutPolicy.TryGet(call.Command, out var timeout, out var owner).ShouldBeTrue();
        timeout.ShouldBe(TimeSpan.FromSeconds(3));
        owner.ShouldBe("secret manager 'MsSqlSecrets'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnUpdateBuiltWithoutOneCarriesNone()
    {
        var call = Update.In<TestEntity>("Secret").DataStore("TestDb").Path("dbo").Where("Id", 1).Value(new TestEntity { Id = 1 });

        CommandTimeoutPolicy.TryGet(call.Command, out _, out _).ShouldBeFalse();
    }
}
