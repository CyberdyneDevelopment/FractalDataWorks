using System;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Validators;

namespace Fdw.Services.Notifications.Tests.Validators;

/// <summary>
/// Tests for <see cref="NotificationRuleConfigurationValidator"/>: required fields, the
/// closed-set <c>ConditionOperator</c>/<c>Severity</c> checks, the conditional
/// <c>CooldownMinutes</c> rule, and the conditional safe-string check on <c>Description</c>.
/// </summary>
public sealed class NotificationRuleConfigurationValidatorTests
{
    private readonly NotificationRuleConfigurationValidator _sut = new();

    private static NotificationRuleConfiguration ValidConfig() => new()
    {
        Name = "Rule1",
        NotificationServiceType = "Teams",
        NotificationServiceName = "OpsTeams",
        ConditionOperator = "And",
        Severity = "Error",
    };

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateSucceedsWhenAllRequiredFieldsArePresentAndValid()
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
        result.Errors.ShouldContain(e => e.PropertyName == nameof(NotificationRuleConfiguration.Name));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWhenNotificationServiceTypeIsEmpty()
    {
        // Arrange
        var config = ValidConfig();
        config.NotificationServiceType = string.Empty;

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(NotificationRuleConfiguration.NotificationServiceType));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWhenNotificationServiceNameIsEmpty()
    {
        // Arrange
        var config = ValidConfig();
        config.NotificationServiceName = string.Empty;

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(NotificationRuleConfiguration.NotificationServiceName));
    }

    [Theory]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    [InlineData("And")]
    [InlineData("Or")]
    public void ValidateSucceedsForEachValidConditionOperator(string conditionOperator)
    {
        // Arrange
        var config = ValidConfig();
        config.ConditionOperator = conditionOperator;

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    [InlineData("Xor")]
    [InlineData("")]
    [InlineData("and")]
    public void ValidateFailsForAnyConditionOperatorOutsideTheClosedSet(string conditionOperator)
    {
        // Arrange
        var config = ValidConfig();
        config.ConditionOperator = conditionOperator;

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(NotificationRuleConfiguration.ConditionOperator));
    }

    [Theory]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    [InlineData("Info")]
    [InlineData("Warning")]
    [InlineData("Error")]
    [InlineData("Critical")]
    public void ValidateSucceedsForEachValidSeverity(string severity)
    {
        // Arrange
        var config = ValidConfig();
        config.Severity = severity;

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWhenSeverityIsOutsideTheClosedSet()
    {
        // Arrange
        var config = ValidConfig();
        config.Severity = "Debug";

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(NotificationRuleConfiguration.Severity));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateSucceedsWhenCooldownMinutesIsNull()
    {
        // Arrange
        var config = ValidConfig();
        config.CooldownMinutes = null;

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWhenCooldownMinutesIsLessThanOne()
    {
        // Arrange
        var config = ValidConfig();
        config.CooldownMinutes = 0;

        // Act
        var result = _sut.Validate(config);

        // Assert — FluentValidation reports the full member path for `x.CooldownMinutes!.Value`
        // (nullable unwrap), so the property name is "CooldownMinutes.Value", not "CooldownMinutes".
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.StartsWith(nameof(NotificationRuleConfiguration.CooldownMinutes), StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateSucceedsWhenCooldownMinutesIsAtLeastOne()
    {
        // Arrange
        var config = ValidConfig();
        config.CooldownMinutes = 1;

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateSucceedsWhenDescriptionIsNull()
    {
        // Arrange
        var config = ValidConfig();
        config.Description = null;

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWhenDescriptionContainsControlCharacters()
    {
        // Arrange
        var config = ValidConfig();
        config.Description = "line1line2";

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(NotificationRuleConfiguration.Description));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWhenDescriptionExceedsMaximumLength()
    {
        // Arrange
        var config = ValidConfig();
        config.Description = new string('a', 1001);

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(NotificationRuleConfiguration.Description));
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Configuration")]
    public void ValidateSucceedsWhenDescriptionIsASafeStringWithinTheLengthLimit()
    {
        // Arrange
        var config = ValidConfig();
        config.Description = "Notifies the on-call rotation when a pipeline fails.";

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }
}
