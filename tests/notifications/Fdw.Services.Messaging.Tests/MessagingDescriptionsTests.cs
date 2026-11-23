using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Messaging.Abstractions;
using Fdw.Services.Messaging.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Messaging.Tests;

/// <summary>
/// A messaging configuration's description lives in the descriptions table and not on <c>msg.MessagingConfiguration</c> (FDW-7203, a group of FDW-6208): the provider
/// describes the configuration in the unit of its save, fills the text once for every set of composed configurations, and removes it with the configuration. The
/// writer and reader are strict stand-ins; <c>DescriptionServiceTests</c> covers what they do.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class MessagingDescriptionsTests
{
    private static readonly Guid First = Guid.Parse("00000000-0000-0000-0000-0000000007c1");
    private static readonly Guid Second = Guid.Parse("00000000-0000-0000-0000-0000000007c2");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private MessagingConfigurationProvider Provider()
        => new(
            NullLogger<MessagingConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object, "PlatformConfiguration", "msg", "Messaging",
            _writer.Object, _reader.Object);

    private static MessagingImplementationConfiguration Record(Guid id, string name, string? text = null)
        => new() { Id = id, Name = name, Description = text };

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheWriteAndReadObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var provider = Provider();

        provider.ShouldBeAssignableTo<IConfigurationWriteObserver<IMessagingImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationReadObserver<IMessagingImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IReferencedConfigurationProvider<IMessagingImplementationConfiguration>>();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("Where this deployment keeps its messages")]
    [InlineData(null)]
    [InlineData("")]
    public async Task ASavedConfigurationIsDescribedUnderTheMessagingTypeWithTheTextItCarriesOrNone(string? text)
    {
        _writer
            .Setup(w => w.Describe("Messaging", "Text", First, "Messaging", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Provider().Written(Record(First, "Messaging", text), null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionFailsTheSaveSoItsUnitRollsBack()
    {
        _writer
            .Setup(w => w.Describe("Messaging", "Text", First, "Messaging", "Where this deployment keeps its messages", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Provider().Written(Record(First, "Messaging", "Where this deployment keeps its messages"), null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryComposedConfigurationAndOneWithNoneStaysEmpty()
    {
        var things = new List<IMessagingImplementationConfiguration> { Record(First, "Messaging"), Record(Second, "Other") };
        _reader
            .Setup(r => r.Fill("Messaging", "Text", It.IsAny<IReadOnlyList<IMessagingImplementationConfiguration>>(), It.IsAny<Func<IMessagingImplementationConfiguration, Guid>>(),
                It.IsAny<Action<IMessagingImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<IMessagingImplementationConfiguration>, Func<IMessagingImplementationConfiguration, Guid>, Action<IMessagingImplementationConfiguration, string>, CancellationToken>(
                (_, _, found, idOf, fill, _) =>
                {
                    foreach (var thing in found)
                    {
                        if (idOf(thing) == First) fill(thing, "Where this deployment keeps its messages");
                    }
                })
            .ReturnsAsync(GenericResult.Success());

        var read = await Provider().Read(things, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        things[0].Description.ShouldBe("Where this deployment keeps its messages");
        things[1].Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheReadOfComposedConfigurations()
    {
        _reader
            .Setup(r => r.Fill("Messaging", "Text", It.IsAny<IReadOnlyList<IMessagingImplementationConfiguration>>(), It.IsAny<Func<IMessagingImplementationConfiguration, Guid>>(),
                It.IsAny<Action<IMessagingImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var read = await Provider().Read([Record(First, "Messaging")], null, TestContext.Current.CancellationToken);

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
            .Setup(r => r.Fill("Messaging", "Text", It.IsAny<IReadOnlyList<IMessagingImplementationConfiguration>>(), It.IsAny<Func<IMessagingImplementationConfiguration, Guid>>(),
                It.IsAny<Action<IMessagingImplementationConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());

        await Provider().Read([Record(First, "Messaging")], null, cancelled.Token);

        _reader.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingAConfigurationRemovesItsDescriptionsInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("Messaging", First, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        (await Provider().RemoveReferences(First, null!, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _writer.VerifyAll();
    }
}
