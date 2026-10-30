using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Configuration;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Services.Notifications.Configuration;

namespace Fdw.Services.Notifications.Tests;

/// <summary>
/// Tests for <see cref="NotificationDomainConfiguration"/> (FDW-1182): the notifications domain row carries the fields
/// the domain owns, so a channel's domain row is never written without them.
/// </summary>
/// <remarks>
/// The domain provider finds a domain's list fields as the record's mapper properties beyond the four every domain has,
/// then takes their values from the implementation when it writes the row. With the plain record the set was empty, so
/// the row was written without <c>IsEnabled</c> and the table refused it. These tests state the two halves of that:
/// the record declares the fields, and the implementation a channel is saved from has every one of them.
/// </remarks>
public sealed class NotificationDomainConfigurationTests
{
    private static readonly string[] OwnedByTheDomainRow = ["IsEnabled", "SecretManagerName", "SecretKeyName", "Description"];

    private static List<string> ListFieldsOf(string recordName)
    {
        var plain = PocoMapperCollection.ByName(nameof(DomainConfiguration)).GetPropertyNames();
        return PocoMapperCollection.ByName(recordName).GetPropertyNames()
            .Where(name => !plain.Contains(name, StringComparer.Ordinal))
            .ToList();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Notifications")]
    public void TheDomainRecordListsExactlyTheFieldsTheDomainRowOwns()
    {
        _ = new NotificationDomainConfiguration();

        ListFieldsOf(nameof(NotificationDomainConfiguration)).OrderBy(name => name, StringComparer.Ordinal)
            .ShouldBe(OwnedByTheDomainRow.OrderBy(name => name, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Priority", "P0")]
    [Trait("Category", "Notifications")]
    public void TheDomainRowIsWrittenWithTheChannelsIsEnabled(bool isEnabled)
    {
        var row = new NotificationDomainConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "ops",
            Domain = "Notification",
            Implementation = "System",
            IsEnabled = isEnabled,
        };

        var written = PocoMapperCollection.ByName(nameof(NotificationDomainConfiguration)).MapToParameters(row);

        written["IsEnabled"].ShouldBe(isEnabled);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Notifications")]
    public void TheDomainRowCarriesTheSecretNamesWhenTheChannelStatesThemAndNothingWhenItDoesNot()
    {
        var mapper = PocoMapperCollection.ByName(nameof(NotificationDomainConfiguration));

        var stated = mapper.MapToParameters(new NotificationDomainConfiguration { SecretManagerName = "vault", SecretKeyName = "ops-key" });
        var unstated = mapper.MapToParameters(new NotificationDomainConfiguration());

        stated["SecretManagerName"].ShouldBe("vault");
        stated["SecretKeyName"].ShouldBe("ops-key");
        unstated["SecretManagerName"].ShouldBeNull();
        unstated["SecretKeyName"].ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Notifications")]
    public void TheSystemChannelCarriesEveryFieldTheDomainRowOwns()
    {
        var channel = new global::Fdw.Services.Notifications.System.SystemNotificationConfiguration();

        var carried = PocoMapperCollection.ByName(channel.GetType().Name).GetPropertyNames();

        foreach (var field in OwnedByTheDomainRow)
            carried.ShouldContain(field);
    }
}
