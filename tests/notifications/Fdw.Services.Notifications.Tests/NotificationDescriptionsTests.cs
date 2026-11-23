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
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Notifications.Tests;

/// <summary>
/// A notification's description lives in the descriptions table and not on <c>notify.Notification</c> (FDW-7203, a group of FDW-6208): the provider describes the notification in the unit of
/// its save, fills the text once for every set of composed notifications and once for every page of domain records it reads, and removes it with the notification. The writer and
/// reader are strict stand-ins; <c>DescriptionServiceTests</c> covers what they do.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class NotificationDescriptionsTests
{
    private static readonly Guid First = Guid.Parse("00000000-0000-0000-0000-0000000007a1");
    private static readonly Guid Second = Guid.Parse("00000000-0000-0000-0000-0000000007a2");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private NotificationConfigurationProvider Provider()
        => new(
            NullLogger<NotificationConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object, "PlatformConfiguration", "notify", "Notification",
            _writer.Object, _reader.Object);

    private static Fdw.Services.Notifications.System.SystemNotificationConfiguration Record(Guid id, string name, string? text = null)
        => new() { Id = id, Name = name, Description = text };

    // The reader answers with the texts it holds, put on each record through the provider's own Id and fill delegates.
    private void TheReaderHolds<T>(Dictionary<Guid, string> texts)
        => _reader
            .Setup(r => r.Fill("Notification", "Text", It.IsAny<IReadOnlyList<T>>(), It.IsAny<Func<T, Guid>>(), It.IsAny<Action<T, string>>(), It.IsAny<CancellationToken>()))
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
            .Setup(r => r.Fill("Notification", "Text", It.IsAny<IReadOnlyList<T>>(), It.IsAny<Func<T, Guid>>(), It.IsAny<Action<T, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheWriteReadAndPageObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var provider = Provider();

        provider.ShouldBeAssignableTo<IConfigurationWriteObserver<INotificationImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationReadObserver<INotificationImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationDomainReadObserver<NotificationDomainConfiguration>>();
        provider.ShouldBeAssignableTo<IReferencedConfigurationProvider<INotificationImplementationConfiguration>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheDomainRecordsMapperHasNoDescriptionSoNoReadSelectsTheColumnAndNoSaveWritesIt()
    {
        var mapper = PocoMapperCollection.ByName(nameof(NotificationDomainConfiguration));

        mapper.ShouldNotBeSameAs(PocoMapperCollection.NotFound);
        mapper.GetPropertyNames().ShouldNotContain("Description");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("Where a dev failure is announced")]
    [InlineData(null)]
    [InlineData("")]
    public async Task ASavedNotificationIsDescribedUnderTheTypeWithTheTextItCarriesOrNone(string? text)
    {
        _writer
            .Setup(w => w.Describe("Notification", "Text", First, "DevConsole", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Provider().Written(Record(First, "DevConsole", text), null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionFailsTheSaveSoItsUnitRollsBack()
    {
        _writer
            .Setup(w => w.Describe("Notification", "Text", First, "DevConsole", "Where a dev failure is announced", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Provider().Written(Record(First, "DevConsole", "Where a dev failure is announced"), null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryComposedNotificationAndOneWithNoneStaysEmpty()
    {
        var things = new List<INotificationImplementationConfiguration> { Record(First, "DevConsole"), Record(Second, "other") };
        TheReaderHolds<INotificationImplementationConfiguration>(new Dictionary<Guid, string> { [First] = "Where a dev failure is announced" });

        var read = await Provider().Read(things, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        things[0].Description.ShouldBe("Where a dev failure is announced");
        things[1].Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryNotificationOfAListPage()
    {
        var page = new List<NotificationDomainConfiguration> { new() { Id = First, Name = "DevConsole" }, new() { Id = Second, Name = "other" } };
        TheReaderHolds<NotificationDomainConfiguration>(new Dictionary<Guid, string> { [Second] = "Mail to the on-call list" });

        var read = await Provider().Read(page, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        page[0].Description.ShouldBeNull();
        page[1].Description.ShouldBe("Mail to the on-call list");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheReadOfAPageAndOfComposedNotifications()
    {
        TheReaderFails<NotificationDomainConfiguration>();
        TheReaderFails<INotificationImplementationConfiguration>();

        var page = await Provider().Read([new NotificationDomainConfiguration { Id = First, Name = "DevConsole" }], TestContext.Current.CancellationToken);
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
            .Setup(r => r.Fill("Notification", "Text", It.IsAny<IReadOnlyList<INotificationImplementationConfiguration>>(), It.IsAny<Func<INotificationImplementationConfiguration, Guid>>(), It.IsAny<Action<INotificationImplementationConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());
        _reader
            .Setup(r => r.Fill("Notification", "Text", It.IsAny<IReadOnlyList<NotificationDomainConfiguration>>(), It.IsAny<Func<NotificationDomainConfiguration, Guid>>(), It.IsAny<Action<NotificationDomainConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());

        await Provider().Read([Record(First, "DevConsole")], null, cancelled.Token);
        await Provider().Read([new NotificationDomainConfiguration { Id = First, Name = "DevConsole" }], cancelled.Token);

        _reader.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingANotificationRemovesItsDescriptionsInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("Notification", First, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        (await Provider().RemoveReferences(First, null!, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _writer.VerifyAll();
    }
}
