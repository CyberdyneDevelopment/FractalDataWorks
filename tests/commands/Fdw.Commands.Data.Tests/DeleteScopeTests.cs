using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Tests;

/// <summary>
/// FDW-3660: a delete states the rows it removes: a filter, soft-delete options, or AllRows. One that states none, or that
/// has a filter and also asks for every row, is refused, so no translator sends a delete with no condition by accident.
/// </summary>
public sealed class DeleteScopeTests
{
    private static FilterExpression ById() => new() { Root = new FilterCondition { PropertyName = "Id", Operator = new EqualOperator(), Value = 42 } };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADeleteWithAFilterMayRun()
        => DeleteScope.Problem(new DeleteCommand { Filter = ById() }).ShouldBeNull();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADeleteThatAsksForEveryRowMayRun()
        => DeleteScope.Problem(new DeleteCommand { AllRows = true }).ShouldBeNull();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASoftDeleteWithNoFilterMayRun()
        => DeleteScope.Problem(new DeleteCommand { SoftDelete = new SoftDeleteOptions("IsDeleted", SoftDeleteModes.FilterMatches) { FlagValue = true } }).ShouldBeNull();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADeleteThatStatesNoRowsIsRefused()
        => DeleteScope.Problem(new DeleteCommand()).ShouldNotBeNull().ShouldContain("AllRows()");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADeleteWithAnEmptyFilterIsRefused()
        => DeleteScope.Problem(new DeleteCommand { Filter = new FilterExpression { Root = null } }).ShouldNotBeNull().ShouldContain("AllRows()");

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ADeleteWithAFilterThatAlsoAsksForEveryRowIsRefused()
        => DeleteScope.Problem(new DeleteCommand { Filter = ById(), AllRows = true }).ShouldNotBeNull().ShouldContain("also asks for every row");

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AFilterableCommandThatCannotAskForEveryRowIsRefusedWithNoFilterAndRunsWithOne()
    {
        var unscoped = new Mock<IFilterableCommand>();
        var filtered = new Mock<IFilterableCommand>();
        filtered.Setup(c => c.Filter).Returns(ById());

        DeleteScope.Problem(unscoped.Object).ShouldNotBeNull();
        DeleteScope.Problem(filtered.Object).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ADeleteDoesNotAskForEveryRowUnlessToldTo()
        => new DeleteCommand().AllRows.ShouldBeFalse();
}
