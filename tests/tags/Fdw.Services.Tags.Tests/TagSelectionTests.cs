using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Tags.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tags.Tests;

/// <summary>What a list does with <c>?tag=</c>: no tag leaves the list alone, tags keep what carries them all, and a host without the service refuses (FDW-3816).</summary>
[Trait("Category", "Tags")]
public sealed class TagSelectionTests
{
    private static readonly Guid A = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid B = Guid.Parse("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid C = Guid.Parse("00000000-0000-0000-0000-00000000000c");

    private static Mock<ITagService> ServiceFinding(params Guid[] things)
    {
        var tags = new Mock<ITagService>();
        tags
            .Setup(t => t.ThingsOf(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<Guid>>.Success(things));
        return tags;
    }

    private static Task<IGenericResult<ConfigurationPage<Guid>>> AllOf(int skip, int take, CancellationToken token)
    {
        var rows = new[] { A, B, C }.Skip(skip).Take(take).ToList();
        return Task.FromResult<IGenericResult<ConfigurationPage<Guid>>>(GenericResult<ConfigurationPage<Guid>>.Success(
            new ConfigurationPage<Guid>(skip, take, skip + rows.Count < 3, _ => ToStream(rows))));
    }

    private static async IAsyncEnumerable<Guid> ToStream(IReadOnlyList<Guid> rows)
    {
        foreach (var row in rows)
        {
            yield return row;
        }

        await Task.CompletedTask;
    }

    private static async Task<List<Guid>> Items(ConfigurationPage<Guid> page)
    {
        var items = new List<Guid>();
        await foreach (var item in page.Items)
        {
            items.Add(item);
        }

        return items;
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARequestThatNamesNoTagLeavesTheListAlone()
    {
        var selection = await TagSelection.Read([], null, "Pipeline", NullLogger.Instance, TestContext.Current.CancellationToken);

        selection.IsSuccess.ShouldBeTrue();
        selection.Value!.Applies.ShouldBeFalse();
        selection.Value.Keeps(A).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARequestThatNamesTagsToAHostWithoutTheServiceIsRefused()
    {
        var selection = await TagSelection.Read(["finance"], null, "Pipeline", NullLogger.Instance, TestContext.Current.CancellationToken);

        selection.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TagsKeepOnlyTheThingsTheServiceFound()
    {
        var tags = ServiceFinding(B);

        var selection = await TagSelection.Read(["finance", "q3"], tags.Object, "DataSet", NullLogger.Instance, TestContext.Current.CancellationToken);

        selection.Value!.Applies.ShouldBeTrue();
        selection.Value.Keeps(B).ShouldBeTrue();
        selection.Value.Keeps(A).ShouldBeFalse();
        tags.Verify(t => t.ThingsOf("DataSet", It.Is<IReadOnlyList<string>>(names => names.SequenceEqual(new[] { "finance", "q3" })), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AServiceFailureIsTheFailureOfTheSelection()
    {
        var tags = new Mock<ITagService>();
        tags
            .Setup(t => t.ThingsOf(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<Guid>>.Failure(TagsResultCodes.ByName("TagNotFound"), NullLogger.Instance, ResultDetails.Create("name", "nope")));

        var selection = await TagSelection.Read(["nope"], tags.Object, "DataSet", NullLogger.Instance, TestContext.Current.CancellationToken);

        selection.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task WithNoTagThePageIsTheListsOwnPage()
    {
        var page = await TagSelection.None.Read(AllOf, id => id, 1, 1, TestContext.Current.CancellationToken);

        page.IsSuccess.ShouldBeTrue();
        (await Items(page.Value!)).ShouldBe([B]);
        page.Value!.HasMore.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task WithTagsThePageIsCutFromTheMembersThatCarryThem()
    {
        var selection = await TagSelection.Read(["finance"], ServiceFinding(A, C).Object, "Pipeline", NullLogger.Instance, TestContext.Current.CancellationToken);

        var first = await selection.Value!.Read(AllOf, id => id, 0, 1, TestContext.Current.CancellationToken);
        var second = await selection.Value.Read(AllOf, id => id, 1, 1, TestContext.Current.CancellationToken);

        (await Items(first.Value!)).ShouldBe([A]);
        first.Value!.HasMore.ShouldBeTrue();
        first.Value.Total.ShouldBe(2);
        (await Items(second.Value!)).ShouldBe([C]);
        second.Value!.HasMore.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task WhenNoMemberCarriesTheTagsThePageIsEmpty()
    {
        var selection = await TagSelection.Read(["finance"], ServiceFinding().Object, "Pipeline", NullLogger.Instance, TestContext.Current.CancellationToken);

        var page = await selection.Value!.Read(AllOf, id => id, 0, 10, TestContext.Current.CancellationToken);

        (await Items(page.Value!)).ShouldBeEmpty();
        page.Value!.HasMore.ShouldBeFalse();
        page.Value.Total.ShouldBe(0);
    }
}
