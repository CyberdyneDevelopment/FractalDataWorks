using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Validators;

namespace Fdw.Services.Notifications.Tests.Validators;

/// <summary>
/// Tests for <see cref="NotificationRecipientConfigurationValidator"/>.
/// </summary>
public sealed class NotificationRecipientConfigurationValidatorTests
{
    private readonly NotificationRecipientConfigurationValidator _sut = new();

    private static NotificationRecipientConfiguration ValidConfig() => new()
    {
        Name = "Email:oncall@example.com",
        Recipient = "oncall@example.com",
        RecipientType = "Email",
    };

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateSucceedsWhenAllRequiredFieldsArePresent()
    {
        // Act
        var result = _sut.Validate(ValidConfig());

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWhenNameIsEmpty()
    {
        // Arrange
        var config = ValidConfig();
        config.Name = string.Empty;

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(NotificationRecipientConfiguration.Name));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWhenRecipientIsEmpty()
    {
        // Arrange
        var config = ValidConfig();
        config.Recipient = string.Empty;

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(NotificationRecipientConfiguration.Recipient));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWhenRecipientTypeIsEmpty()
    {
        // Arrange
        var config = ValidConfig();
        config.RecipientType = string.Empty;

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(NotificationRecipientConfiguration.RecipientType));
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Configuration")]
    public void ValidateReportsAllMissingRequiredFieldsAtOnce()
    {
        // Arrange
        var config = new NotificationRecipientConfiguration
        {
            Name = string.Empty,
            Recipient = string.Empty,
            RecipientType = string.Empty,
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(3);
    }
}
