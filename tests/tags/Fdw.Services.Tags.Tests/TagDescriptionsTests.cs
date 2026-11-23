using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Tags.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tags.Tests;

/// <summary>
/// A tag's description lives in the descriptions table and not in <c>tag.TagConfiguration</c> (FDW-6208): the tag provider describes the tag in the unit of
/// its save, fills the text once for every set it reads, and removes it with the tag. The writer and reader are stand-ins, so what is asserted is what the
/// provider asks of them and what it does with their answer; <c>DescriptionServiceTests</c> covers what they do.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class TagDescriptionsTests
{
    private static readonly Guid Urgent = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid Archive = Guid.Parse("00000000-0000-0000-0000-0000000000a2");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private TagConfigurationProvider Provider()
        => new(
            NullLogger<TagConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>().Object, "PlatformConfiguration", "tag", "Tag",
            _writer.Object, _reader.Object);

    private static TagImplementationConfiguration Tag(Guid id, string name, string? description = null)
        => new() { Id = id, Name = name, Description = description, OwnerKind = "Tenant", OwnerId = Guid.Parse("00000000-0000-0000-0000-0000000000e1") };

    // The reader answers with the texts it holds, put on each tag through the provider's own Id and fill delegates.
    private void TheReaderHolds(Dictionary<Guid, string> texts)
        => _reader
            .Setup(r => r.Fill(
                "Tag", "Text", It.IsAny<IReadOnlyList<ITagImplementationConfiguration>>(), It.IsAny<Func<ITagImplementationConfiguration, Guid>>(),
                It.IsAny<Action<ITagImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<ITagImplementationConfiguration>, Func<ITagImplementationConfiguration, Guid>, Action<ITagImplementationConfiguration, string>, CancellationToken>(
                (_, _, tags, idOf, fill, _) =>
                {
                    foreach (var tag in tags)
                    {
                        if (texts.TryGetValue(idOf(tag), out var text)) fill(tag, text);
                    }
                })
            .ReturnsAsync(GenericResult.Success());

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheWriteAndReadObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var provider = Provider();

        provider.ShouldBeAssignableTo<IConfigurationWriteObserver<ITagImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationReadObserver<ITagImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IReferencedConfigurationProvider<ITagImplementationConfiguration>>();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("Needs an answer today")]
    [InlineData(null)]
    [InlineData("")]
    public async Task ASavedTagIsDescribedUnderTheTagTypeWithTheTextItCarriesOrNone(string? text)
    {
        _writer
            .Setup(w => w.Describe("Tag", "Text", Urgent, "urgent", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Provider().Written(Tag(Urgent, "urgent", text), null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionFailsTheSaveSoItsUnitRollsBack()
    {
        _writer
            .Setup(w => w.Describe("Tag", "Text", Urgent, "urgent", "Needs an answer today", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Provider().Written(Tag(Urgent, "urgent", "Needs an answer today"), null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryTagOfTheSetAndATagWithNoneStaysEmpty()
    {
        var tags = new List<ITagImplementationConfiguration> { Tag(Urgent, "urgent"), Tag(Archive, "archive") };
        TheReaderHolds(new Dictionary<Guid, string> { [Urgent] = "Needs an answer today" });

        var read = await Provider().Read(tags, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        tags[0].Description.ShouldBe("Needs an answer today");
        tags[1].Description.ShouldBeNull();
        _reader.Verify(
            r => r.Fill(
                "Tag", "Text", It.IsAny<IReadOnlyList<ITagImplementationConfiguration>>(), It.IsAny<Func<ITagImplementationConfiguration, Guid>>(),
                It.IsAny<Action<ITagImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheTagRead()
    {
        _reader
            .Setup(r => r.Fill(
                "Tag", "Text", It.IsAny<IReadOnlyList<ITagImplementationConfiguration>>(), It.IsAny<Func<ITagImplementationConfiguration, Guid>>(),
                It.IsAny<Action<ITagImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var read = await Provider().Read([Tag(Urgent, "urgent")], null, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task TheReadPassesTheCallersTokenToTheFill()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        _reader
            .Setup(r => r.Fill(
                "Tag", "Text", It.IsAny<IReadOnlyList<ITagImplementationConfiguration>>(), It.IsAny<Func<ITagImplementationConfiguration, Guid>>(),
                It.IsAny<Action<ITagImplementationConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());

        await Provider().Read([Tag(Urgent, "urgent")], null, cancelled.Token);

        _reader.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingATagRemovesItsDescriptionsInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("Tag", Urgent, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        var removed = await Provider().RemoveReferences(Urgent, null!, TestContext.Current.CancellationToken);

        removed.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }
}
