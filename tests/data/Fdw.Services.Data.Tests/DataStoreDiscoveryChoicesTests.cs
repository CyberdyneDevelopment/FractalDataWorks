using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A discovery's choices come from its request and none is defaulted (FDW-3823): a request that leaves one out is refused, naming it,
/// and an empty excluded list is a stated choice.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "Configuration")]
public sealed class DataStoreDiscoveryChoicesTests
{
    private static readonly string[] Excluded = ["sys"];

    [Fact]
    public void EveryStatedChoiceIsCarried()
    {
        var options = DataStoreDiscoveryChoices.FromRequest(Excluded, ["dbo"], true, false, true, false, "OpsDb", NullLogger.Instance);

        options.IsSuccess.ShouldBeTrue();
        options.Value!.ExcludedSchemas.ShouldBe(Excluded);
        options.Value.IncludeOnlySchemas.ShouldBe(["dbo"]);
        options.Value.DiscoverViews.ShouldBeTrue();
        options.Value.DiscoverIndexes.ShouldBeFalse();
        options.Value.DiscoverForeignKeys.ShouldBeTrue();
        options.Value.DiscoverDescriptions.ShouldBeFalse();
    }

    [Fact]
    public void AnEmptyExcludedListAndNoIncludeListAreStatedChoices()
    {
        var options = DataStoreDiscoveryChoices.FromRequest(Array.Empty<string>(), null, false, false, false, false, "OpsDb", NullLogger.Instance);

        options.IsSuccess.ShouldBeTrue();
        options.Value!.ExcludedSchemas.ShouldBeEmpty();
        options.Value.IncludeOnlySchemas.ShouldBeNull();
    }

    public static TheoryData<string> Choices => new() { "ExcludedSchemas", "DiscoverViews", "DiscoverIndexes", "DiscoverForeignKeys", "DiscoverDescriptions" };

    [Theory]
    [MemberData(nameof(Choices))]
    public void AChoiceLeftOutIsRefusedByName(string leftOut)
    {
        var options = DataStoreDiscoveryChoices.FromRequest(
            leftOut == "ExcludedSchemas" ? null : Excluded,
            null,
            leftOut == "DiscoverViews" ? null : true,
            leftOut == "DiscoverIndexes" ? null : true,
            leftOut == "DiscoverForeignKeys" ? null : true,
            leftOut == "DiscoverDescriptions" ? null : true,
            "OpsDb",
            NullLogger.Instance);

        options.IsSuccess.ShouldBeFalse();
        options.CurrentMessage.ShouldNotBeNull().ShouldContain(leftOut);
    }
}
