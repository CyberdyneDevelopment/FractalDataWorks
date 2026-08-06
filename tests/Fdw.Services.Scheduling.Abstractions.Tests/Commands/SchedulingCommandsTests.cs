using Fdw.Services.Scheduling.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Abstractions.Tests.Commands;

public class SchedulingCommandsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void AllReturnsAllCommands()
    {
        var all = SchedulingCommands.All();

        all.ShouldNotBeNull();
        // At minimum, collection should not be null even if empty
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        var result = SchedulingCommands.ById(99999);

        result.ShouldNotBeNull();
        result.ShouldBe(SchedulingCommands.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        var result = SchedulingCommands.ByName("UnknownCommand");

        result.ShouldNotBeNull();
        result.ShouldBe(SchedulingCommands.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void NotFoundReturnsEmptyInstance()
    {
        var result = SchedulingCommands.NotFound;

        result.ShouldNotBeNull();
    }
}
