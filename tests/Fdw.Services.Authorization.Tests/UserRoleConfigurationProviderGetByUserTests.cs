using System;
using System.Threading.Tasks;
using Fdw.Services.Authorization.Configuration;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// Regression tests for reading a user's role assignments by user id (FDW-532 follow-up).
///
/// Root cause guarded here: <c>authz.UserRole.UserId</c> is stored UPPERCASE in the DB, while the
/// token subject GUID arrives lowercase (Guid.ToString() emits lowercase). A case-SENSITIVE compare
/// matches nobody, so EVERY user (admin included) gets ZERO roles -> 0 permissions -> a 401/403
/// cascade.
/// </summary>
/// <remarks>
/// The provider no longer owns this comparison. <c>GetByUser</c> was a specialized verb and is gone;
/// a caller now states its own predicate through <c>Find</c>. So the guarantee these tests exist for
/// has MOVED to the call sites, and it only holds if each of them compares with
/// <see cref="StringComparison.OrdinalIgnoreCase"/> -- a predicate written as <c>ur.UserId == userId</c>
/// is an Ordinal compare and reintroduces FDW-532 exactly. That is what these tests now pin: the
/// predicate a caller must write, exercised against the REAL provider over a faked store.
/// </remarks>
public class UserRoleConfigurationProviderGetByUserTests
{
    // The live admin Id from the verified production row, shown in both cases.
    private const string AdminIdUpper = "CA520AE5-1234-4ABC-9DEF-0123456789AB";
    private static readonly string AdminIdLower = AdminIdUpper.ToLowerInvariant();

    private static UserRoleConfigurationProvider MakeProvider(params IUserRoleImplementationConfiguration[] storedRows)
        => ConfigurationCatalog.UserRoles(storedRows);

    private static UserRoleImplementationConfiguration Assignment(string userId)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = $"{userId}:role",
            UserId = userId,
            RoleId = Guid.NewGuid(),
        };

    /// <summary>The predicate a caller must supply for a by-user read to survive a case difference.</summary>
    private static Func<IUserRoleImplementationConfiguration, bool> ForUser(string userId)
        => assignment => string.Equals(assignment.UserId, userId, StringComparison.OrdinalIgnoreCase);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authorization")]
    [Trait("Issue", "FDW-532")]
    public async Task FindByUserMatchesWhenStoredUpperAndQueriedLower()
    {
        // Arrange: stored row is UPPERCASE (as the DB stores it); query is lowercase (token subject).
        var provider = MakeProvider(Assignment(AdminIdUpper));

        // Act
        var result = await provider.Find(ForUser(AdminIdLower), TestContext.Current.CancellationToken);

        // Assert: the assignment IS returned despite the case difference.
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(1);
        result.Value[0].UserId.ShouldBe(AdminIdUpper);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authorization")]
    [Trait("Issue", "FDW-532")]
    public async Task FindByUserMatchesWhenStoredLowerAndQueriedUpper()
    {
        // Arrange: reverse direction — stored lowercase, queried uppercase.
        var provider = MakeProvider(Assignment(AdminIdLower));

        // Act
        var result = await provider.Find(ForUser(AdminIdUpper), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(1);
        result.Value[0].UserId.ShouldBe(AdminIdLower);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authorization")]
    [Trait("Issue", "FDW-532")]
    public async Task FindByUserReturnsAllAssignmentsForUserAcrossCase()
    {
        // Arrange: a user with two role assignments stored UPPERCASE, plus an unrelated user's row.
        var other = Guid.NewGuid().ToString().ToUpperInvariant();
        var provider = MakeProvider(
            Assignment(AdminIdUpper),
            Assignment(AdminIdUpper),
            Assignment(other));

        // Act: query lowercase.
        var result = await provider.Find(ForUser(AdminIdLower), TestContext.Current.CancellationToken);

        // Assert: exactly the two admin assignments, none of the other user's.
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(2);
        result.Value.ShouldAllBe(ur => string.Equals(ur.UserId, AdminIdUpper, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Authorization")]
    [Trait("Issue", "FDW-532")]
    public async Task FindByUserReturnsEmptyForGenuinelyDifferentUser()
    {
        var provider = MakeProvider(Assignment(AdminIdUpper));

        var result = await provider.Find(
            ForUser(Guid.NewGuid().ToString()), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeEmpty();
    }

    /// <summary>
    /// The regression itself, stated directly: an Ordinal predicate is what FDW-532 was.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authorization")]
    [Trait("Issue", "FDW-532")]
    public async Task FindByUserWithAnOrdinalPredicateMissesTheUserEntirely()
    {
        var provider = MakeProvider(Assignment(AdminIdUpper));

        var result = await provider.Find<IUserRoleImplementationConfiguration>(
            assignment => assignment.UserId == AdminIdLower, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeEmpty(
            "an Ordinal compare on UserId is precisely the FDW-532 defect; callers must use OrdinalIgnoreCase");
    }
}
