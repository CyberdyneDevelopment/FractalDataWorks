using System;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Endpoints;

namespace Fdw.Services.Notifications.Tests.Services;

/// <summary>
/// FDW-1237: a notification stored enabled reads back enabled. The read of one and the list of many both answer what the domain row and
/// the implementation record store, not the defaults of the answer.
/// </summary>
public sealed class NotificationMapperTests
{
    private sealed class StoredNotification : INotificationImplementationConfiguration
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Domain { get; set; } = string.Empty;

        public string Implementation { get; set; } = string.Empty;

        public bool IsEnabled { get; set; }

        public string? SecretManagerName { get; set; }

        public string? SecretKeyName { get; set; }

        public string? Description { get; set; }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ADetailOfANotificationStoredEnabledIsEnabled()
    {
        var stored = new StoredNotification { Id = Guid.NewGuid(), Name = "Alerts", Implementation = "Console", IsEnabled = true };

        var detail = NotificationMapper.ToDetail(stored);

        detail.IsEnabled.ShouldBeTrue();
        detail.Id.ShouldBe(stored.Id);
        detail.Name.ShouldBe("Alerts");
        detail.Implementation.ShouldBe("Console");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ADetailOfANotificationStoredDisabledIsDisabled()
    {
        var detail = NotificationMapper.ToDetail(new StoredNotification { Name = "Alerts", IsEnabled = false });

        detail.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ADetailCarriesTheDescriptionItWasStoredWith()
    {
        var detail = NotificationMapper.ToDetail(new StoredNotification { Name = "Alerts", Description = "pages the on-call" });

        detail.Description.ShouldBe("pages the on-call");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ASummaryCarriesTheDescriptionOfItsDomainRow()
    {
        var summary = NotificationMapper.ToSummary(new NotificationDomainConfiguration { Name = "Alerts", Description = "pages the on-call" });

        summary.Description.ShouldBe("pages the on-call");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ADetailCarriesTheSecretNamesItWasStoredWith()
    {
        var detail = NotificationMapper.ToDetail(new StoredNotification
        {
            Name = "Alerts",
            SecretManagerName = "EnvSecrets",
            SecretKeyName = "ALERTS_TOKEN"
        });

        detail.SecretManagerName.ShouldBe("EnvSecrets");
        detail.SecretKeyName.ShouldBe("ALERTS_TOKEN");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ASummaryOfANotificationStoredEnabledIsEnabled()
    {
        var row = new NotificationDomainConfiguration { Id = Guid.NewGuid(), Name = "Alerts", Implementation = "Email", IsEnabled = true };

        var summary = NotificationMapper.ToSummary(row);

        summary.IsEnabled.ShouldBeTrue();
        summary.Name.ShouldBe("Alerts");
        summary.Implementation.ShouldBe("Email");
    }
}
