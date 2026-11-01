using System.Linq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Notifications.Tests.Services;

/// <summary>
/// FDW-2931: the severities a rule may carry are one collection. A severity not in it is refused when the rule is saved, and the priority
/// delivery sends with is read from the same collection.
/// </summary>
public sealed class NotificationRuleSeveritiesTests
{
    [Theory]
    [InlineData("Info", "Normal")]
    [InlineData("warning", "High")]
    [InlineData("ERROR", "Critical")]
    [InlineData("Low", "Low")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ASeverityOfferedMapsToItsPriorityWithoutRegardToCase(string severity, string priority)
    {
        NotificationRuleSeverities.Offers(severity).ShouldBeTrue();
        NotificationRuleSeverities.TryPriority(severity, out var found).ShouldBeTrue();
        found.ShouldBe(priority);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Loud")]
    [InlineData(null)]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ASeverityNotOfferedIsRefusedAndHasNoPriority(string? severity)
    {
        NotificationRuleSeverities.Offers(severity).ShouldBeFalse();
        NotificationRuleSeverities.TryPriority(severity, out var found).ShouldBeFalse();
        found.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EverySeverityServedIsOneTheSaveOffers()
    {
        NotificationRuleSeverities.All().ShouldNotBeEmpty();
        NotificationRuleSeverities.All().ShouldAllBe(pair => NotificationRuleSeverities.Offers(pair.Key));
        NotificationRuleSeverities.All().Select(pair => pair.Key).Distinct().Count().ShouldBe(NotificationRuleSeverities.All().Count);
    }
}
