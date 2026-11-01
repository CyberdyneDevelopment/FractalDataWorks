using System;
using System.Data;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.MsSql;

namespace Fdw.Services.Connections.Sql.Tests;

/// <summary>
/// A tenant run (FDW-1087) is governed by its own session context: never a user, never system, never deny,
/// and it sets exactly the keys row-level security's tenant-run branch reads.
/// </summary>
public sealed class TenantRunSessionContextTests
{
    private static readonly Guid PipelineId = new("44444444-4444-4444-4444-444444444444");
    private static readonly Guid TenantId = new("11111111-1111-1111-1111-111111111111");

    private static TenantRunAuthenticationContext Run()
        => new(PipelineId, "NightlyLoad", TenantId, ["DataWriter"], ["datasets:read", "datasets:write"]);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ATenantRunIsGovernedByTheTenantRunContext()
        => MsSqlSessionContextTypes.For(Run()).Name.ShouldBe("TenantRun");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ATenantRunIsNeitherSystemNorAUser()
    {
        var plan = MsSqlSessionContextTypes.For(Run()).Plan(Run());

        plan.IsSystem.ShouldBeFalse();
        plan.IsTenantRun.ShouldBeTrue();
        plan.UserId.ShouldBe(PipelineId);
        plan.TenantId.ShouldBe(TenantId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ATenantRunSetsItsPrincipalItsTenantAndTheTenantRunKey()
    {
        var statement = MsSqlSessionContextTypes.For(Run()).Statement(SessionContextPlan.ForTenantRun(PipelineId, TenantId), string.Empty);

        statement.Sql.ShouldBe(
            "EXEC sp_set_session_context @key = N'UserId', @value = @userId, @read_only = 1;"
            + " EXEC sp_set_session_context @key = N'TenantId', @value = @tenantId, @read_only = 1;"
            + " EXEC sp_set_session_context @key = N'TenantRun', @value = N'1', @read_only = 1;");
        statement.Parameters.ShouldBe(
        [
            new SessionContextParameter("userId", SqlDbType.UniqueIdentifier, PipelineId),
            new SessionContextParameter("tenantId", SqlDbType.UniqueIdentifier, TenantId),
        ]);
        statement.Sql.ShouldNotContain("System");
        statement.Sql.ShouldNotContain("CanReadSecrets");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ATenantRunAndAUserWithTheSameIdNeverShareACacheEntry()
    {
        var user = new Moq.Mock<IAuthenticationContext>();
        user.SetupGet(u => u.UserId).Returns(PipelineId.ToString());
        user.SetupGet(u => u.IsAuthenticated).Returns(true);
        user.SetupGet(u => u.ActiveTenantId).Returns(TenantId);
        user.SetupGet(u => u.Permissions).Returns([]);

        MsSqlSessionContextTypes.For(Run()).CachePartition(Run())
            .ShouldNotBe(MsSqlSessionContextTypes.For(user.Object).CachePartition(user.Object));
    }
}
