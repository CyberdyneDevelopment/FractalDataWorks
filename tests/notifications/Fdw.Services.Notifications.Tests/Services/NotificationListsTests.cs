using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Notifications.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Notifications.Tests.Services;

/// <summary>
/// FDW-2931: the notification lists are what the store holds, with the number of enabled members of each. A store that holds none answers
/// none, and a store that cannot be read answers its failure, not an empty list. A list's description is not a column (FDW-7203): it is read from the descriptions
/// table, once for the lists the read made, and a description that cannot be read fails the read.
/// </summary>
public sealed class NotificationListsTests
{
    private static readonly Guid Oncall = new("11111111-0000-0000-0000-000000000001");
    private static readonly Guid Finance = new("11111111-0000-0000-0000-000000000002");

    // A reader that holds no description: every fill succeeds and leaves each list as it is.
    private static IDescriptionReader NoDescriptions()
    {
        var reader = new Mock<IDescriptionReader>(MockBehavior.Strict);
        reader
            .Setup(r => r.Fill("NotificationList", "Text", It.IsAny<IReadOnlyList<NotificationListConfiguration>>(),
                It.IsAny<Func<NotificationListConfiguration, Guid>>(), It.IsAny<Action<NotificationListConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        return reader.Object;
    }

    // A reader that holds the texts given, put on each list through the reader's own Id and fill delegates.
    private static IDescriptionReader Descriptions(Dictionary<Guid, string> texts)
    {
        var reader = new Mock<IDescriptionReader>(MockBehavior.Strict);
        reader
            .Setup(r => r.Fill("NotificationList", "Text", It.IsAny<IReadOnlyList<NotificationListConfiguration>>(),
                It.IsAny<Func<NotificationListConfiguration, Guid>>(), It.IsAny<Action<NotificationListConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<NotificationListConfiguration>, Func<NotificationListConfiguration, Guid>, Action<NotificationListConfiguration, string>, CancellationToken>(
                (_, _, things, idOf, fill, _) =>
                {
                    foreach (var thing in things)
                    {
                        if (texts.TryGetValue(idOf(thing), out var text)) fill(thing, text);
                    }
                })
            .ReturnsAsync(GenericResult.Success());
        return reader.Object;
    }

    private static NotificationLists Create(
        IReadOnlyList<NotificationListConfiguration> lists, IReadOnlyList<NotificationListMemberConfiguration> members, bool membersFail = false)
        => Create(lists, members, NoDescriptions(), membersFail);

    private static NotificationLists Create(
        IReadOnlyList<NotificationListConfiguration> lists, IReadOnlyList<NotificationListMemberConfiguration> members, IDescriptionReader reader, bool membersFail = false)
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<NotificationListConfiguration>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<NotificationListConfiguration>>.Success(lists));
        gateway.Setup(g => g.Execute<IEnumerable<NotificationListMemberConfiguration>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(membersFail
                ? GenericResult<IEnumerable<NotificationListMemberConfiguration>>.Failure(new GenericMessage("the members table is down"))
                : GenericResult<IEnumerable<NotificationListMemberConfiguration>>.Success(members));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get("PlatformConfiguration")).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        return new NotificationLists(gateways.Object, "PlatformConfiguration", reader, NullLogger<NotificationLists>.Instance);
    }

    private static NotificationListMemberConfiguration Member(Guid list, bool enabled)
        => new() { Id = Guid.NewGuid(), NotificationListId = list, Name = "m", Recipient = "r", RecipientType = "Email", IsEnabled = enabled };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AStoreThatHoldsNoListsAnswersNone()
    {
        var lists = Create([], []);

        var listed = await lists.List(TestContext.Current.CancellationToken);

        listed.IsSuccess.ShouldBeTrue();
        listed.Value!.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TheListsAreInNameOrderEachWithItsEnabledMembers()
    {
        var lists = Create(
            [new NotificationListConfiguration { Id = Oncall, Name = "on-call", IsEnabled = true },
             new NotificationListConfiguration { Id = Finance, Name = "finance", IsEnabled = false }],
            [Member(Oncall, true), Member(Oncall, true), Member(Oncall, false), Member(Finance, true)],
            Descriptions(new Dictionary<Guid, string> { [Oncall] = "pages" }));

        var listed = await lists.List(TestContext.Current.CancellationToken);

        listed.IsSuccess.ShouldBeTrue();
        listed.Value!.Select(list => list.Name).ShouldBe(["finance", "on-call"]);
        listed.Value![0].MemberCount.ShouldBe(1);
        listed.Value![0].IsEnabled.ShouldBeFalse();
        listed.Value![1].MemberCount.ShouldBe(2);
        listed.Value![1].Description.ShouldBe("pages");
        listed.Value![0].Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ADescriptionThatCannotBeReadFailsTheReadAndNamesTheReason()
    {
        var reader = new Mock<IDescriptionReader>(MockBehavior.Strict);
        reader
            .Setup(r => r.Fill("NotificationList", "Text", It.IsAny<IReadOnlyList<NotificationListConfiguration>>(),
                It.IsAny<Func<NotificationListConfiguration, Guid>>(), It.IsAny<Action<NotificationListConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new GenericMessage("language not stated")));
        var lists = Create([new NotificationListConfiguration { Id = Oncall, Name = "on-call" }], [], reader.Object);

        var listed = await lists.List(TestContext.Current.CancellationToken);

        listed.IsFailure.ShouldBeTrue();
        listed.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TheCallersTokenReachesTheDescriptionFill()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var reader = new Mock<IDescriptionReader>(MockBehavior.Strict);
        reader
            .Setup(r => r.Fill("NotificationList", "Text", It.IsAny<IReadOnlyList<NotificationListConfiguration>>(),
                It.IsAny<Func<NotificationListConfiguration, Guid>>(), It.IsAny<Action<NotificationListConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());
        var lists = Create([new NotificationListConfiguration { Id = Oncall, Name = "on-call" }], [], reader.Object);

        await lists.List(cancelled.Token);

        reader.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AStoreThatCannotBeReadAnswersItsFailureNotAnEmptyList()
    {
        var lists = Create([new NotificationListConfiguration { Id = Oncall, Name = "on-call" }], [], membersFail: true);

        var listed = await lists.List(TestContext.Current.CancellationToken);

        listed.IsFailure.ShouldBeTrue();
    }
}
