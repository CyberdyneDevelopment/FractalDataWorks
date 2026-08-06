using Fdw.Messages;
using Fdw.Services.Scheduling.Abstractions.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Abstractions.Tests.Messages;

public class SchedulingMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void JobScheduledMessageCreatesWithDefaultConstructor()
    {
        // Act
        var message = new JobScheduledMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("scheduled successfully");
        message.Severity.ShouldBe(MessageSeverity.Information);
        message.Code.ShouldBe("SCHED_JOB_SCHEDULED");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void JobScheduledMessageCreatesWithJobId()
    {
        // Act
        var message = new JobScheduledMessage("job-123");

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("job-123");
        message.Message.ShouldContain("scheduled successfully");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void JobFailedMessageCreatesWithDefaultConstructor()
    {
        // Act
        var message = new JobFailedMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("failed");
        message.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void JobFailedMessageCreatesWithJobIdAndReason()
    {
        // Act
        var message = new JobFailedMessage("job-123", "Connection timeout");

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("job-123");
        message.Message.ShouldContain("Connection timeout");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void JobCompletedMessageCreatesWithDefaultConstructor()
    {
        // Act
        var message = new JobCompletedMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("completed");
        message.Severity.ShouldBe(MessageSeverity.Information);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void JobCompletedMessageCreatesWithJobIdAndDuration()
    {
        // Act
        var message = new JobCompletedMessage("job-123", 1500);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("job-123");
        message.Message.ShouldContain("1500");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void TriggerIdNullOrEmptyMessageCreates()
    {
        // Act
        var message = new TriggerIdNullOrEmptyMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Trigger ID");
        message.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void TriggerNameNullOrEmptyMessageCreates()
    {
        // Act
        var message = new TriggerNameNullOrEmptyMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Trigger name");
        message.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void TriggerTypeNullOrEmptyMessageCreates()
    {
        // Act
        var message = new TriggerTypeNullOrEmptyMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Trigger type");
        message.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void TriggerConfigurationNullMessageCreates()
    {
        // Act
        var message = new TriggerConfigurationNullMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Trigger configuration");
        message.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void TriggerNullMessageCreates()
    {
        // Act
        var message = new TriggerNullMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Trigger");
        message.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void InvalidTimestampMessageCreates()
    {
        // Act
        var message = new InvalidTimestampMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("timestamp");
        message.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ScheduleIdNullOrEmptyMessageCreates()
    {
        // Act
        var message = new ScheduleIdNullOrEmptyMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Schedule ID");
        message.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ScheduleNameNullOrEmptyMessageCreates()
    {
        // Act
        var message = new ScheduleNameNullOrEmptyMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Schedule name");
        message.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ProcessIdNullOrEmptyMessageCreates()
    {
        // Act
        var message = new ProcessIdNullOrEmptyMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Process ID");
        message.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ProcessTypeNullOrEmptyMessageCreates()
    {
        // Act
        var message = new ProcessTypeNullOrEmptyMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Process type");
        message.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ProcessConfigurationNullMessageCreates()
    {
        // Act
        var message = new ProcessConfigurationNullMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("Process configuration");
        message.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void InvalidScheduleTimestampMessageCreates()
    {
        // Act
        var message = new InvalidScheduleTimestampMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("timestamp");
        message.Severity.ShouldBe(MessageSeverity.Error);
    }
}
