using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Notifications.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Notifications.Tests;

/// <summary>
/// A notification rule's description lives in the descriptions table and not on <c>notify.NotificationRule</c> (FDW-7203, a group of FDW-6208): the provider describes the rule in the unit of
/// its save, fills the text once for every set of composed rules and once for every page of domain records it reads, and removes it with the rule. The writer and
/// reader are strict stand-ins; <c>DescriptionServiceTests</c> covers what they do.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class NotificationRuleDescriptionsTests
{
    private static readonly Guid First = Guid.Parse("00000000-0000-0000-0000-0000000007b1");
    private static readonly Guid Second = Guid.Parse("00000000-0000-0000-0000-0000000007b2");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private NotificationRuleConfigurationProvider Provider()
        => new(
            NullLogger<NotificationRuleConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object, "PlatformConfiguration", "notify", "NotificationRule",
            _writer.Object, _reader.Object);

    private static NotificationRuleImplementationConfiguration Record(Guid id, string name, string? text = null)
        => new() { Id = id, Name = name, Description = text };

    // The reader answers with the texts it holds, put on each record through the provider's own Id and fill delegates.
    private void TheReaderHolds<T>(Dictionary<Guid, string> texts)
        => _reader
            .Setup(r => r.Fill("NotificationRule", "Text", It.IsAny<IReadOnlyList<T>>(), It.IsAny<Func<T, Guid>>(), It.IsAny<Action<T, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<T>, Func<T, Guid>, Action<T, string>, CancellationToken>(
                (_, _, things, idOf, fill, _) =>
                {
                    foreach (var thing in things)
                    {
                        if (texts.TryGetValue(idOf(thing), out var text)) fill(thing, text);
                    }
                })
            .ReturnsAsync(GenericResult.Success());

    private void TheReaderFails<T>()
        => _reader
            .Setup(r => r.Fill("NotificationRule", "Text", It.IsAny<IReadOnlyList<T>>(), It.IsAny<Func<T, Guid>>(), It.IsAny<Action<T, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheWriteReadAndPageObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var provider = Provider();

        provider.ShouldBeAssignableTo<IConfigurationWriteObserver<INotificationRuleImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationReadObserver<INotificationRuleImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationDomainReadObserver<NotificationRuleDomainConfiguration>>();
        provider.ShouldBeAssignableTo<IReferencedConfigurationProvider<INotificationRuleImplementationConfiguration>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheDomainRecordsMapperHasNoDescriptionSoNoReadSelectsTheColumnAndNoSaveWritesIt()
    {
        var mapper = PocoMapperCollection.ByName(nameof(NotificationRuleDomainConfiguration));

        mapper.ShouldNotBeSameAs(PocoMapperCollection.NotFound);
        mapper.GetPropertyNames().ShouldNotContain("Description");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("Tells the on-call list when a run fails")]
    [InlineData(null)]
    [InlineData("")]
    public async Task ASavedRuleIsDescribedUnderTheTypeWithTheTextItCarriesOrNone(string? text)
    {
        _writer
            .Setup(w => w.Describe("NotificationRule", "Text", First, "FailedRuns", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Provider().Written(Record(First, "FailedRuns", text), null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionFailsTheSaveSoItsUnitRollsBack()
    {
        _writer
            .Setup(w => w.Describe("NotificationRule", "Text", First, "FailedRuns", "Tells the on-call list when a run fails", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Provider().Written(Record(First, "FailedRuns", "Tells the on-call list when a run fails"), null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryComposedRuleAndOneWithNoneStaysEmpty()
    {
        var things = new List<INotificationRuleImplementationConfiguration> { Record(First, "FailedRuns"), Record(Second, "other") };
        TheReaderHolds<INotificationRuleImplementationConfiguration>(new Dictionary<Guid, string> { [First] = "Tells the on-call list when a run fails" });

        var read = await Provider().Read(things, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        things[0].Description.ShouldBe("Tells the on-call list when a run fails");
        things[1].Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryRuleOfAListPage()
    {
        var page = new List<NotificationRuleDomainConfiguration> { new() { Id = First, Name = "FailedRuns" }, new() { Id = Second, Name = "other" } };
        TheReaderHolds<NotificationRuleDomainConfiguration>(new Dictionary<Guid, string> { [Second] = "Tells finance when a load is late" });

        var read = await Provider().Read(page, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        page[0].Description.ShouldBeNull();
        page[1].Description.ShouldBe("Tells finance when a load is late");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheReadOfAPageAndOfComposedRules()
    {
        TheReaderFails<NotificationRuleDomainConfiguration>();
        TheReaderFails<INotificationRuleImplementationConfiguration>();

        var page = await Provider().Read([new NotificationRuleDomainConfiguration { Id = First, Name = "FailedRuns" }], TestContext.Current.CancellationToken);
        var composed = await Provider().Read([Record(Second, "other")], null, TestContext.Current.CancellationToken);

        page.IsFailure.ShouldBeTrue();
        page.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
        composed.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task EachReadPassesTheCallersTokenToTheFill()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        _reader
            .Setup(r => r.Fill("NotificationRule", "Text", It.IsAny<IReadOnlyList<INotificationRuleImplementationConfiguration>>(), It.IsAny<Func<INotificationRuleImplementationConfiguration, Guid>>(), It.IsAny<Action<INotificationRuleImplementationConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());
        _reader
            .Setup(r => r.Fill("NotificationRule", "Text", It.IsAny<IReadOnlyList<NotificationRuleDomainConfiguration>>(), It.IsAny<Func<NotificationRuleDomainConfiguration, Guid>>(), It.IsAny<Action<NotificationRuleDomainConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());

        await Provider().Read([Record(First, "FailedRuns")], null, cancelled.Token);
        await Provider().Read([new NotificationRuleDomainConfiguration { Id = First, Name = "FailedRuns" }], cancelled.Token);

        _reader.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingARuleRemovesItsDescriptionsInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("NotificationRule", First, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        (await Provider().RemoveReferences(First, null!, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _writer.VerifyAll();
    }
}
