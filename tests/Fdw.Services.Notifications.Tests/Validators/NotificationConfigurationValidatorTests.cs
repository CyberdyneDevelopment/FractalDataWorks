using Fdw.Services.Notifications.Validators;

namespace Fdw.Services.Notifications.Tests.Validators;

/// <summary>
/// Tests for <see cref="NotificationConfigurationValidator"/>.
/// </summary>
public sealed class NotificationConfigurationValidatorTests
{
    private readonly NotificationConfigurationValidator _sut = new();

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWhenNameIsEmpty()
    {
        // Arrange
        var config = new NotificationConfiguration { Name = string.Empty };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(NotificationConfiguration.Name));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateSucceedsWhenNameIsProvided()
    {
        // Arrange
        var config = new NotificationConfiguration { Name = "OpsAlerts" };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateOptionsReturnsFailWhenNameIsEmpty()
    {
        // Arrange
        var config = new NotificationConfiguration { Name = string.Empty };

        // Act
        var result = ((Microsoft.Extensions.Options.IValidateOptions<NotificationConfiguration>)_sut).Validate(null, config);

        // Assert
        result.Failed.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateOptionsReturnsSuccessWhenNameIsProvided()
    {
        // Arrange
        var config = new NotificationConfiguration { Name = "OpsAlerts" };

        // Act
        var result = ((Microsoft.Extensions.Options.IValidateOptions<NotificationConfiguration>)_sut).Validate(null, config);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }
}
