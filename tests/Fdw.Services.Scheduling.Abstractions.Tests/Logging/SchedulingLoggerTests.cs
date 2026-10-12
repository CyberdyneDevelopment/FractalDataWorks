using Fdw.Services.Scheduling.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Abstractions.Tests.Logging;

public class SchedulingLoggerTests
{
    private readonly ILogger _logger = NullLogger.Instance;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void TriggerIdNullOrEmptyReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.TriggerIdNullOrEmpty(_logger);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Trigger ID");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void TriggerNameNullOrEmptyReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.TriggerNameNullOrEmpty(_logger);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Trigger name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void TriggerTypeNullOrEmptyReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.TriggerTypeNullOrEmpty(_logger);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Trigger type");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void TriggerConfigurationNullReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.TriggerConfigurationNull(_logger);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Trigger configuration");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void InvalidTimestampReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.InvalidTimestamp(_logger);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("timestamp");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ScheduleIdNullOrEmptyReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.ScheduleIdNullOrEmpty(_logger);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Schedule ID");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ScheduleNameNullOrEmptyReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.ScheduleNameNullOrEmpty(_logger);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Schedule name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ProcessIdNullOrEmptyReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.ProcessIdNullOrEmpty(_logger);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Process ID");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ProcessTypeNullOrEmptyReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.ProcessTypeNullOrEmpty(_logger);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Process type");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ProcessConfigurationNullReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.ProcessConfigurationNull(_logger);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Process configuration");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void TriggerNullReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.TriggerNull(_logger);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Trigger");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void InvalidScheduleTimestampReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.InvalidScheduleTimestamp(_logger);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("timestamp");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void JobScheduledWithoutParametersReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.JobScheduled(_logger);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("scheduled");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void JobScheduledWithJobIdReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.JobScheduled(_logger, "job-123");

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("job-123");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void JobFailedWithoutParametersReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.JobFailed(_logger);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void JobFailedWithDetailsReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.JobFailed(_logger, "job-123", "Connection timeout");

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("job-123");
        message.Message.ShouldContain("Connection timeout");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void JobCompletedWithoutParametersReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.JobCompleted(_logger);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("completed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void JobCompletedWithDetailsReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.JobCompleted(_logger, "job-123", 1500);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("job-123");
        message.Message.ShouldContain("1500");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ConfigurationValueMustBeBooleanReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.ConfigurationValueMustBeBoolean(_logger, "AllowConcurrent");

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("AllowConcurrent");
        message.Message.ShouldContain("boolean");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ConfigurationValueMustBeStringReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.ConfigurationValueMustBeString(_logger, "CronExpression");

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("CronExpression");
        message.Message.ShouldContain("string");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CronExpressionRequiredReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.CronExpressionRequired(_logger, "CronExpression");

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Cron expression");
        message.Message.ShouldContain("CronExpression");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void InvalidCronExpressionFormatReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.InvalidCronExpressionFormat(_logger, "Too few fields", "0 9 *");

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Too few fields");
        message.Message.ShouldContain("0 9 *");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void InvalidCronExpressionReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.InvalidCronExpression(_logger, "Invalid day", "0 9 * * 8");

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Invalid day");
        message.Message.ShouldContain("0 9 * * 8");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void InvalidTimezoneIdentifierReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.InvalidTimezoneIdentifier(_logger, "Invalid/Timezone");

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Invalid/Timezone");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void InvalidTimezoneConfigurationReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.InvalidTimezoneConfiguration(_logger, "Unknown timezone", "BadZone");

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Unknown timezone");
        message.Message.ShouldContain("BadZone");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CronExpressionWillNeverExecuteReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.CronExpressionWillNeverExecute(_logger, "0 0 31 2 *");

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("0 0 31 2 *");
        message.Message.ShouldContain("never execute");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CronExpressionValidationFailedReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.CronExpressionValidationFailed(_logger, "Parse error", "bad cron");

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Parse error");
        message.Message.ShouldContain("bad cron");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void InvalidStartTimeReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.InvalidStartTime(_logger, "StartTime");

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Start time");
        message.Message.ShouldContain("StartTime");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void StartTimeTooFarInPastReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.StartTimeTooFarInPast(_logger, "2024-01-01T00:00:00Z");

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("2024-01-01T00:00:00Z");
        message.Message.ShouldContain("past");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void IntervalRequiredReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.IntervalRequired(_logger, "IntervalMinutes");

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Interval");
        message.Message.ShouldContain("IntervalMinutes");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void IntervalMustBePositiveReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.IntervalMustBePositive(_logger, 0);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("0");
        message.Message.ShouldContain("greater than");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void UnknownTriggerTypeReturnsMessage()
    {
        // Act
        var message = SchedulingLogger.UnknownTriggerType(_logger, "CustomType");

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("CustomType");
        message.Message.ShouldContain("Unknown");
    }
}
