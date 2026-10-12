using System;
using Fdw.Services.Authorization.Configuration;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// Which UserRole rows <see cref="UserRoleAssignments.HeldBy"/> selects for a user (FDW-991).
/// </summary>
public class UserRoleAssignmentsTests
{
    private static readonly Guid User = Guid.Parse("ca520ae5-1234-4abc-9def-0123456789ab");

    private static UserRoleImplementationConfiguration Assignment(string userId)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = $"{userId}:role",
            UserId = userId,
            RoleId = Guid.NewGuid(),
        };

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authorization")]
    [InlineData("CA520AE5-1234-4ABC-9DEF-0123456789AB")]
    [InlineData("ca520ae5-1234-4abc-9def-0123456789ab")]
    [InlineData("ca520ae512344abc9def0123456789ab")]
    public void HeldByMatchesTheUsersGuidInAnySpelling(string stored)
        => UserRoleAssignments.HeldBy(User)(Assignment(stored)).ShouldBeTrue();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authorization")]
    public void HeldByDoesNotMatchAServiceIdentity()
        => UserRoleAssignments.HeldBy(User)(Assignment("fdw.scheduler")).ShouldBeFalse();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authorization")]
    public void HeldByDoesNotMatchAnotherUsersGuid()
        => UserRoleAssignments.HeldBy(User)(Assignment(Guid.NewGuid().ToString())).ShouldBeFalse();

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Authorization")]
    public void HeldByDoesNotMatchEmptyText()
        => UserRoleAssignments.HeldBy(User)(Assignment(string.Empty)).ShouldBeFalse();
}
