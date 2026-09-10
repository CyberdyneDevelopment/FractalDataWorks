using Fdw.Services.Notifications.Abstractions;
using System;
using Fdw.Services.Notifications.Validators;

namespace Fdw.Services.Notifications.Tests.Validators;

/// <summary>
/// Tests for <see cref="NotificationConfigurationValidator"/>.
/// </summary>
public sealed class NotificationConfigurationValidatorTests
{
    // Why a double: the validator validates the domain's contract, and every notification
    // implementation is supplied by reference-servicetypes, so there is no concrete one here.
    private sealed class TestNotificationConfiguration : INotificationImplementationConfiguration
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Domain { get; set; } = string.Empty;
    }

    private readonly NotificationConfigurationValidator _sut = new();

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWhenNameIsEmpty()
    {
        // Arrange
        var config = new TestNotificationConfiguration { Name = string.Empty };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(INotificationImplementationConfiguration.Name));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateSucceedsWhenNameIsProvided()
    {
        // Arrange
        var config = new TestNotificationConfiguration { Name = "OpsAlerts" };

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
        var config = new TestNotificationConfiguration { Name = string.Empty };

        // Act
        var result = ((Microsoft.Extensions.Options.IValidateOptions<INotificationImplementationConfiguration>)_sut).Validate(null, config);

        // Assert
        result.Failed.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateOptionsReturnsSuccessWhenNameIsProvided()
    {
        // Arrange
        var config = new TestNotificationConfiguration { Name = "OpsAlerts" };

        // Act
        var result = ((Microsoft.Extensions.Options.IValidateOptions<INotificationImplementationConfiguration>)_sut).Validate(null, config);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }
}
