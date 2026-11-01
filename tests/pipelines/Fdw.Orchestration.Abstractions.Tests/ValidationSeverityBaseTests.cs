using Fdw.Orchestration.Abstractions.TypeCollections.ValidationSeverityOptions;

namespace Fdw.Orchestration.Abstractions.Tests;

public class ValidationSeverityBaseTests
{
    private sealed class TestValidationSeverity : ValidationSeverityBase
    {
        public TestValidationSeverity(
            int id,
            string name,
            int level,
            bool blocksExecution,
            bool requiresAcknowledgment = false,
            bool shouldLog = true)
            : base(id, name, level, blocksExecution, requiresAcknowledgment, shouldLog)
        {
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsAllProperties()
    {
        var severity = new TestValidationSeverity(
            1, "Error",
            level: 100,
            blocksExecution: true,
            requiresAcknowledgment: true,
            shouldLog: true);

        severity.Id.ShouldBe(1);
        severity.Name.ShouldBe("Error");
        severity.Level.ShouldBe(100);
        severity.BlocksExecution.ShouldBeTrue();
        severity.RequiresAcknowledgment.ShouldBeTrue();
        severity.ShouldLog.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void WarningSeverityDoesNotBlockExecution()
    {
        var severity = new TestValidationSeverity(
            2, "Warning",
            level: 50,
            blocksExecution: false);

        severity.BlocksExecution.ShouldBeFalse();
        severity.RequiresAcknowledgment.ShouldBeFalse();
        severity.ShouldLog.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void InfoSeverityDefaultsToLogging()
    {
        var severity = new TestValidationSeverity(
            3, "Info",
            level: 10,
            blocksExecution: false);

        severity.ShouldLog.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void SeverityCanDisableLogging()
    {
        var severity = new TestValidationSeverity(
            4, "Trace",
            level: 1,
            blocksExecution: false,
            shouldLog: false);

        severity.ShouldLog.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void SeverityLevelSupportsOrdering()
    {
        var info = new TestValidationSeverity(1, "Info", level: 10, blocksExecution: false);
        var warning = new TestValidationSeverity(2, "Warning", level: 50, blocksExecution: false);
        var error = new TestValidationSeverity(3, "Error", level: 100, blocksExecution: true);

        error.Level.ShouldBeGreaterThan(warning.Level);
        warning.Level.ShouldBeGreaterThan(info.Level);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultOptionalParametersAreCorrect()
    {
        var severity = new TestValidationSeverity(
            1, "Default",
            level: 0,
            blocksExecution: false);

        severity.RequiresAcknowledgment.ShouldBeFalse();
        severity.ShouldLog.ShouldBeTrue();
    }
}
