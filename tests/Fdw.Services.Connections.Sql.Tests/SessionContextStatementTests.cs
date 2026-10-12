using System;
using System.Linq;
using System.Data;
using System.Text.RegularExpressions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.MsSql;

namespace Fdw.Services.Connections.Sql.Tests;

/// <summary>
/// The SQL each session context sets its keys with (FDW-977). <see cref="MsSqlSessionContextBase.Apply"/> and
/// <see cref="BatchedSessionContext"/> both take it from <see cref="MsSqlSessionContextBase.Statement"/>, so
/// what is pinned here is what every path sets. The literals below are the SQL <c>Apply</c> ran before it
/// was made to read the statement, so a change to them is a change to what a connection is allowed to see.
/// </summary>
public sealed class SessionContextStatementTests
{
    private static readonly Guid UserId = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TenantId = new("11111111-1111-1111-1111-111111111111");

    private const string SetSystem = "EXEC sp_set_session_context @key = N'System', @value = N'1', @read_only = 1;";
    private const string SetUser = "EXEC sp_set_session_context @key = N'UserId', @value = @userId, @read_only = 1;";
    private const string SetTenant = " EXEC sp_set_session_context @key = N'TenantId', @value = @tenantId, @read_only = 1;";

    private static MsSqlSessionContextBase Context(string name)
        => MsSqlSessionContextTypes.All().OfType<MsSqlSessionContextBase>().Single(c => string.Equals(c.Name, name, StringComparison.Ordinal));

    private static SessionContextStatement SystemStatement(Guid? tenantId, string prefix)
        => Context("SystemContext").Statement(SessionContextPlan.ForSystem(tenantId), prefix);

    private static SessionContextStatement UserStatement(Guid? tenantId, string prefix)
        => Context("ForUser").Statement(SessionContextPlan.ForUser(UserId, tenantId), prefix);

    private static SessionContextStatement DenyStatement(string prefix)
        => Context("Deny").Statement(SessionContextPlan.Deny("no context was established"), prefix);

    public static TheoryData<string> Prefixes => new() { string.Empty, BatchedSessionContext.ParameterPrefix };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SystemWithoutATenantSetsOnlyTheSystemKey()
    {
        var statement = SystemStatement(null, string.Empty);

        statement.Sql.ShouldBe(SetSystem);
        statement.Parameters.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SystemForATenantSetsTheSystemKeyThenTheTenantKey()
    {
        var statement = SystemStatement(TenantId, string.Empty);

        statement.Sql.ShouldBe(SetSystem + SetTenant);
        statement.Parameters.ShouldBe(
            [new SessionContextParameter("tenantId", SqlDbType.UniqueIdentifier, TenantId)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AUserSetsItsUserKeyAndNothingElseWhenItHasNoTenant()
    {
        var statement = UserStatement(null, string.Empty);

        statement.Sql.ShouldBe(SetUser);
        statement.Parameters.ShouldBe(
            [new SessionContextParameter("userId", SqlDbType.UniqueIdentifier, UserId)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AUserInATenantSetsUserThenTenantAndNoOtherKey()
    {
        // No key lets a user read a Restricted row, whatever the token carries (FDW-1086).
        var statement = UserStatement(TenantId, string.Empty);

        statement.Sql.ShouldBe(SetUser + SetTenant);
        statement.Sql.ShouldNotContain("CanReadSecrets");
        statement.Parameters.ShouldBe(
        [
            new SessionContextParameter("userId", SqlDbType.UniqueIdentifier, UserId),
            new SessionContextParameter("tenantId", SqlDbType.UniqueIdentifier, TenantId),
        ]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TheDenyPrincipalSetsOnlyTheReservedNoAccessUser()
    {
        var statement = DenyStatement(string.Empty);

        statement.Sql.ShouldBe(SetUser);
        statement.Parameters.ShouldBe(
            [new SessionContextParameter("userId", SqlDbType.UniqueIdentifier, AuthConstants.NoAccessPrincipalId)]);
    }

    [Theory]
    [MemberData(nameof(Prefixes))]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void EveryKeyEveryContextSetsIsReadOnly(string prefix)
    {
        SessionContextStatement[] statements =
        [
            SystemStatement(null, prefix),
            SystemStatement(TenantId, prefix),
            UserStatement(null, prefix),
            UserStatement(TenantId, prefix),
            DenyStatement(prefix),
        ];

        foreach (var statement in statements)
        {
            // A read-only key cannot be set twice on a session, and cannot be changed by the command.
            Occurrences(statement.Sql, "sp_set_session_context").ShouldBeGreaterThan(0);
            Occurrences(statement.Sql, "@read_only = 1").ShouldBe(Occurrences(statement.Sql, "sp_set_session_context"));
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ThePrefixReachesTheSqlAndTheParameterNamesAndNothingUnprefixedRemains()
    {
        var statement = UserStatement(TenantId, BatchedSessionContext.ParameterPrefix);

        statement.Sql.ShouldContain("@__fdwctx_userId");
        statement.Sql.ShouldContain("@__fdwctx_tenantId");
        statement.Sql.ShouldNotContain("@userId");
        statement.Sql.ShouldNotContain("@tenantId");
        statement.Parameters.Select(p => p.Name).ShouldBe(["__fdwctx_userId", "__fdwctx_tenantId"]);
    }

    [Theory]
    [MemberData(nameof(Prefixes))]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void TheParametersTheSqlNamesAreExactlyTheOnesDescribed(string prefix)
    {
        // Sp arguments are @key, @value and @read_only; every other @name is one of the statement's
        // own parameters, and each of those must be described so a command can bind it.
        var statement = UserStatement(TenantId, prefix);

        var named = Regex.Matches(statement.Sql, "@([A-Za-z_][A-Za-z0-9_]*)")
            .Select(match => match.Groups[1].Value)
            .Where(name => name is not ("key" or "value" or "read_only"))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        named.ShouldBe(statement.Parameters.Select(parameter => parameter.Name).Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TheGuardedFormWrapsTheStatementsSoAFailureEndsTheBatch()
    {
        var statement = UserStatement(TenantId, BatchedSessionContext.ParameterPrefix);

        statement.Guarded.ShouldStartWith("BEGIN TRY EXEC sp_set_session_context");
        statement.Guarded.ShouldEndWith(" END TRY BEGIN CATCH THROW; END CATCH; ");
        statement.Guarded.ShouldContain(statement.Sql);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TheGuardedFormAddsNoResultSetAndNoRowCountToTheCommandItPrecedes()
    {
        // ExecuteNonQuery's rows-affected and a reader's NextResult both feed callers (a session's
        // "slid == 1" check, a configuration write's key rows). Only statements that return rows or
        // change data could alter them, and the guard has neither.
        var guarded = UserStatement(TenantId, BatchedSessionContext.ParameterPrefix).Guarded;

        foreach (var word in new[] { "SELECT", "OUTPUT", "INSERT", "UPDATE", "DELETE", "MERGE" })
        {
            guarded.ShouldNotContain(word, Case.Insensitive);
        }
    }

    private static int Occurrences(string text, string value)
    {
        var count = 0;
        for (var index = text.IndexOf(value, StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
