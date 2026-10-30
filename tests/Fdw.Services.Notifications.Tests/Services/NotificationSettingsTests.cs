using System;
using System.Collections.Generic;
using System.Text.Json;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.System;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Notifications.Tests.Services;

/// <summary>
/// FDW-2931: the settings of a notification are set by the names its type declares. A setting the type does not have, a value of the wrong
/// type and a secret given as a value are refused naming the setting; a required setting that is missing, or a required secret that is not
/// named, refuses a create.
/// </summary>
public sealed class NotificationSettingsTests
{
    private static readonly NotificationSettingField Severity = new("DefaultSeverity", NotificationSettingTypes.Text, false, false);
    private static readonly NotificationSettingField MessageType = new("DefaultMessageType", NotificationSettingTypes.Text, true, false);
    private static readonly NotificationSettingField Token = new("Token", NotificationSettingTypes.Text, true, true);

    private static INotificationType TypeOf(params NotificationSettingField[] settings)
    {
        var type = new Mock<INotificationType>();
        type.SetupGet(t => t.Name).Returns("System");
        type.SetupGet(t => t.Settings).Returns(settings);
        return type.Object;
    }

    private static Dictionary<string, JsonElement> Given(string name, string json)
        => new(StringComparer.Ordinal) { [name] = JsonDocument.Parse(json).RootElement };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AGivenSettingIsSetOnTheConfigurationByItsName()
    {
        var configuration = new SystemNotificationConfiguration();

        var applied = NotificationSettings.Apply(TypeOf(Severity), configuration, Given("DefaultSeverity", "\"Warning\""), creating: false, NullLogger.Instance);

        applied.IsSuccess.ShouldBeTrue();
        configuration.DefaultSeverity.ShouldBe("Warning");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ASettingTheTypeDoesNotHaveIsRefusedNamingIt()
    {
        var applied = NotificationSettings.Apply(
            TypeOf(Severity), new SystemNotificationConfiguration(), Given("Nonsense", "\"x\""), creating: false, NullLogger.Instance);

        applied.IsFailure.ShouldBeTrue();
        applied.Messages.ShouldContain(message => message.Code == "NOTIFICATION-21030");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AValueOfTheWrongTypeIsRefusedAndNothingIsSet()
    {
        var configuration = new SystemNotificationConfiguration();

        var applied = NotificationSettings.Apply(TypeOf(Severity), configuration, Given("DefaultSeverity", "5"), creating: false, NullLogger.Instance);

        applied.IsFailure.ShouldBeTrue();
        applied.Messages.ShouldContain(message => message.Code == "NOTIFICATION-21031");
        configuration.DefaultSeverity.ShouldBe("Info");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ASecretSettingGivenAsAValueIsRefused()
    {
        var applied = NotificationSettings.Apply(
            TypeOf(Token), new SystemNotificationConfiguration(), Given("Token", "\"hunter2\""), creating: false, NullLogger.Instance);

        applied.IsFailure.ShouldBeTrue();
        applied.Messages.ShouldContain(message => message.Code == "NOTIFICATION-21032");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ACreateWithoutARequiredSettingIsRefusedNamingIt()
    {
        var applied = NotificationSettings.Apply(
            TypeOf(MessageType), new SystemNotificationConfiguration(), new Dictionary<string, JsonElement>(), creating: true, NullLogger.Instance);

        applied.IsFailure.ShouldBeTrue();
        applied.Messages.ShouldContain(message => message.Code == "NOTIFICATION-21033");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ASettingsUpdateDoesNotRequireTheSettingsItLeavesOut()
    {
        var applied = NotificationSettings.Apply(
            TypeOf(MessageType), new SystemNotificationConfiguration(), new Dictionary<string, JsonElement>(), creating: false, NullLogger.Instance);

        applied.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ARequiredSecretMustBeNamedByTheNotificationOnCreate()
    {
        var unnamed = NotificationSettings.Apply(
            TypeOf(Token), new SystemNotificationConfiguration(), new Dictionary<string, JsonElement>(), creating: true, NullLogger.Instance);
        var named = NotificationSettings.Apply(
            TypeOf(Token),
            new SystemNotificationConfiguration { SecretManagerName = "EnvSecrets", SecretKeyName = "TOKEN" },
            new Dictionary<string, JsonElement>(),
            creating: true,
            NullLogger.Instance);

        unnamed.IsFailure.ShouldBeTrue();
        unnamed.Messages.ShouldContain(message => message.Code == "NOTIFICATION-21034");
        named.IsSuccess.ShouldBeTrue();
    }
}
