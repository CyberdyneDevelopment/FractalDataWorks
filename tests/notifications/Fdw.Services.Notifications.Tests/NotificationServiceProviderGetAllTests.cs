using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Fdw.Services.Notifications.Tests;

/// <summary>
/// FDW-3574: <see cref="NotificationServiceProvider"/>'s get-all answers every member its domain configuration provider
/// holds, each built through the implementation the member names. It answered an empty list before.
/// </summary>
public sealed class NotificationServiceProviderGetAllTests
{
    private const string Implementation = "GetAllTestImplementation";

    [Fact]
    public async Task GetAllAnswersEveryStoredMember()
    {
        var first = new Mock<IPlatformNotification>().Object;
        var second = new Mock<IPlatformNotification>().Object;
        var provider = ProviderOver(("first", first), ("second", second));

        var result = await provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe([first, second]);
    }

    [Fact]
    public async Task GetAllFailsWhenAMemberFailsToBuild()
    {
        var provider = ProviderOver(("first", new Mock<IPlatformNotification>().Object), ("unbuildable", null));

        var result = await provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // Each member is a configuration naming one implementation; a null service makes that member's
    // build fail, as an implementation provider that cannot build answers.
    private static NotificationServiceProvider ProviderOver(params (string Name, IPlatformNotification? Service)[] members)
    {
        var configurations = members.Select(m =>
        {
            var configuration = new Mock<INotificationImplementationConfiguration>();
            configuration.SetupProperty(c => c.Name, m.Name);
            configuration.SetupProperty(c => c.Implementation, Implementation);
            return configuration.Object;
        }).ToList();

        var store = new Mock<IDomainConfigurationProvider<INotificationImplementationConfiguration>>();
        store.Setup(s => s.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<INotificationImplementationConfiguration>>.Success(configurations));

        var implementation = new Mock<IImplementationServiceProvider<IPlatformNotification, INotificationImplementationConfiguration>>();
        implementation.Setup(i => i.Create(It.IsAny<INotificationImplementationConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((INotificationImplementationConfiguration configuration, CancellationToken _) =>
                members.Single(m => string.Equals(m.Name, configuration.Name, StringComparison.Ordinal)).Service is { } service
                    ? GenericResult<IPlatformNotification>.Success(service)
                    : GenericResult<IPlatformNotification>.Failure(new Fdw.Messages.GenericMessage("cannot build")));

        var provider = new NotificationServiceProvider(NullLogger<NotificationServiceProvider>.Instance);
        provider.Register(store.Object).IsSuccess.ShouldBeTrue();
        provider.Register(Implementation, () => implementation.Object).IsSuccess.ShouldBeTrue();
        return provider;
    }
}
