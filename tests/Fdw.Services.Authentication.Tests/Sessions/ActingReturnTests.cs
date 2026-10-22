using System;
using System.Linq;
using Fdw.Services.Authentication.Sessions;
using Shouldly;

namespace Fdw.Services.Authentication.Tests.Sessions;

/// <summary>
/// Returning from acting (FDW-2285): the sessions above the step end, top first, and the step wakes.
/// </summary>
public sealed class ActingReturnTests
{
    private static readonly Guid Own = Guid.NewGuid();
    private static readonly Guid First = Guid.NewGuid();
    private static readonly Guid Second = Guid.NewGuid();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ReturningToTheOwnSessionEndsEveryActingSessionTopFirst()
    {
        var plan = ActingReturn.Of([Second, First, Own], 0, out var refusal);

        refusal.ShouldBeNull();
        plan.ShouldNotBeNull();
        plan.Revoke.ToArray().ShouldBe(new[] { Second, First });
        plan.Wake.ShouldBe(Own);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ReturningToAMiddleStepEndsOnlyTheSessionsAboveIt()
    {
        var plan = ActingReturn.Of([Second, First, Own], 1, out _);

        plan.ShouldNotBeNull();
        plan.Revoke.ToArray().ShouldBe(new[] { Second });
        plan.Wake.ShouldBe(First);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ASessionThatIsNotActingHasNothingToReturnTo()
    {
        ActingReturn.Of([Own], 0, out var refusal).ShouldBeNull();

        refusal.ShouldBe("the session is not acting");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(5)]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AStepThatIsNotBelowTheSessionInHandIsRefused(int step)
    {
        ActingReturn.Of([Second, First, Own], step, out var refusal).ShouldBeNull();

        refusal.ShouldNotBeNull();
    }
}
