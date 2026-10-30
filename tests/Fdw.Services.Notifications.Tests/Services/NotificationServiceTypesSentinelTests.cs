using Shouldly;
using Xunit;

namespace Fdw.Services.Notifications.Tests.Services;

/// <summary>
/// FDW-2931: the collection's NotFound sentinel is generated and built with default constructor arguments, so building the collection is
/// the proof that every argument of <see cref="NotificationTypeBase{TService, TFactory, TConfiguration}"/> tolerates a default. A
/// constructor that rejected one aborted every host at start, before it logged.
/// </summary>
public sealed class NotificationServiceTypesSentinelTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void NotFoundSentinelIsBuilt()
    {
        NotificationServiceTypes.NotFound.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void NotFoundSentinelDeclaresNoSettings()
    {
        NotificationServiceTypes.NotFound.Settings.ShouldBeEmpty();
    }
}
