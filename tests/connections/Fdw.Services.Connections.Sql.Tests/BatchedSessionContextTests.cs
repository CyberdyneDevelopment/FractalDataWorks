using System;
using System.Linq;
using System.Data;
using Fdw.Services.Connections.MsSql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Sql.Tests;

/// <summary>
/// A session context sent in the same batch as its command (FDW-977). No SQL Server is involved: what is
/// asserted is the batch the server would be sent, which is what decides whether a command can run
/// without its context, with the wrong one, or with a parameter clobbered.
/// </summary>
public sealed class BatchedSessionContextTests
{
    private const string Select = "SELECT [Id], [Name] FROM [conn].[Connection] WHERE [Name] = @p0";
    private const string EndOfGuard = " END TRY BEGIN CATCH THROW; END CATCH; ";

    private static readonly Guid UserId = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TenantId = new("11111111-1111-1111-1111-111111111111");

    private static MsSqlSessionContextBase Context(string name)
        => MsSqlSessionContextTypes.All().OfType<MsSqlSessionContextBase>().Single(c => string.Equals(c.Name, name, StringComparison.Ordinal));

    public static TheoryData<string, SessionContextPlan> EveryContextVariant() => new()
    {
        { "SystemContext", SessionContextPlan.ForSystem(null) },
        { "SystemContext", SessionContextPlan.ForSystem(TenantId) },
        { "ForUser", SessionContextPlan.ForUser(UserId, null) },
        { "ForUser", SessionContextPlan.ForUser(UserId, TenantId) },
        { "Deny", SessionContextPlan.Deny("no context was established") },
    };

    private static SqlCommand Command(string text = Select)
    {
        var command = new SqlCommand(text);
        command.Parameters.AddWithValue("@p0", "PlatformConfiguration");
        return command;
    }

    private static BatchedSessionContext Attached(string contextName, SessionContextPlan plan, SqlCommand command)
    {
        var attached = BatchedSessionContext.Attach(Context(contextName), plan, command, NullLogger.Instance);
        attached.IsSuccess.ShouldBeTrue();
        return attached.Value.ShouldNotBeNull();
    }

    [Theory]
    [MemberData(nameof(EveryContextVariant))]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TheCommandRunsAfterTheGuardAndIsOtherwiseUnchanged(string contextName, SessionContextPlan plan)
    {
        var command = Command();

        Attached(contextName, plan, command);

        command.CommandText.ShouldStartWith("BEGIN TRY EXEC sp_set_session_context");
        command.CommandText.ShouldEndWith(EndOfGuard + Select);
        command.CommandText.IndexOf(Select, StringComparison.Ordinal).ShouldBe(command.CommandText.IndexOf(EndOfGuard, StringComparison.Ordinal) + EndOfGuard.Length);
        command.CommandText.Split(Select).Length.ShouldBe(2, "the command's own text appears once, after every statement of the context");
    }

    [Theory]
    [MemberData(nameof(EveryContextVariant))]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void NoStatementRunsBeforeOrOutsideTheGuard(string contextName, SessionContextPlan plan)
    {
        var command = Command();

        Attached(contextName, plan, command);

        // Everything that sets a key is inside the one TRY, and the CATCH rethrows: the command that follows
        // is never reached when a key could not be set.
        var guardEnd = command.CommandText.IndexOf(EndOfGuard, StringComparison.Ordinal);
        command.CommandText.LastIndexOf("sp_set_session_context", StringComparison.Ordinal).ShouldBeLessThan(guardEnd);
        command.CommandText.IndexOf("sp_set_session_context", StringComparison.Ordinal).ShouldBe("BEGIN TRY EXEC ".Length);
        command.CommandText.Split("BEGIN TRY").Length.ShouldBe(2);
        command.CommandText.Split("THROW;").Length.ShouldBe(2);
    }

    [Theory]
    [MemberData(nameof(EveryContextVariant))]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void EveryContextParameterIsAddedUnderTheReservedPrefixAndTheCommandsOwnAreKept(string contextName, SessionContextPlan plan)
    {
        var command = Command();

        Attached(contextName, plan, command);

        var statement = Context(contextName).Statement(plan, BatchedSessionContext.ParameterPrefix);
        command.Parameters.Count.ShouldBe(1 + statement.Parameters.Count);
        command.Parameters["@p0"].Value.ShouldBe("PlatformConfiguration");
        foreach (var expected in statement.Parameters)
        {
            var added = command.Parameters["@" + expected.Name];
            added.ParameterName.ShouldStartWith("@" + BatchedSessionContext.ParameterPrefix);
            added.SqlDbType.ShouldBe(expected.Type);
            added.Value.ShouldBe(expected.Value);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ACommandWithItsOwnUserIdAndTenantIdParametersIsNotClobberedByTheContexts()
    {
        // The reason for the prefix. Today's Apply names its parameters @userId and @tenantId; a command
        // that also binds those names would have had one silently replace the other in a shared batch.
        var command = new SqlCommand("SELECT [Id] FROM [auth].[UserConfiguration] WHERE [Id] = @userId AND [TenantId] = @tenantId");
        var commandUser = Guid.NewGuid();
        var commandTenant = Guid.NewGuid();
        command.Parameters.AddWithValue("@userId", commandUser);
        command.Parameters.AddWithValue("@tenantId", commandTenant);

        Attached("ForUser", SessionContextPlan.ForUser(UserId, TenantId), command);

        command.Parameters["@userId"].Value.ShouldBe(commandUser);
        command.Parameters["@tenantId"].Value.ShouldBe(commandTenant);
        command.Parameters["@__fdwctx_userId"].Value.ShouldBe(UserId);
        command.Parameters["@__fdwctx_tenantId"].Value.ShouldBe(TenantId);
        command.Parameters.Count.ShouldBe(4);
    }

    [Theory]
    [InlineData("@__fdwctx_userId")]
    [InlineData("__fdwctx_anything")]
    [InlineData("@__FDWCTX_tenantId")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ACommandThatUsesAReservedParameterNameIsRefusedAndLeftUntouched(string reservedName)
    {
        var command = Command();
        command.Parameters.AddWithValue(reservedName, "x");

        var attached = BatchedSessionContext.Attach(
            Context("ForUser"), SessionContextPlan.ForUser(UserId, TenantId), command, NullLogger.Instance);

        attached.IsSuccess.ShouldBeFalse();
        command.CommandText.ShouldBe(Select);
        command.Parameters.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AStoredProcedureCommandIsRefusedAndLeftUntouched()
    {
        // A stored procedure's text is a procedure name: putting a T-SQL prefix in front of it would not
        // run. Those commands keep the separate Apply, which is the caller's decision to make. (SqlClient
        // itself refuses TableDirect, so a stored procedure is the one other command type there is.)
        var command = new SqlCommand("dbo.SomeProcedure") { CommandType = CommandType.StoredProcedure };

        var attached = BatchedSessionContext.Attach(
            Context("SystemContext"), SessionContextPlan.ForSystem(null), command, NullLogger.Instance);

        attached.IsSuccess.ShouldBeFalse();
        command.CommandText.ShouldBe("dbo.SomeProcedure");
        command.Parameters.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TheBatchAddsNoResultSetAndNoDataChangeToAWrite()
    {
        // A write's rows-affected (a session's "slid == 1") and a configuration write's key rows come from
        // the command's own statements. The context's contribute neither.
        var command = new SqlCommand("UPDATE [auth].[Session] SET [LastSeenAt] = @p0 WHERE [Id] = @p1");
        command.Parameters.AddWithValue("@p0", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("@p1", Guid.NewGuid());
        var original = command.CommandText;

        Attached("ForUser", SessionContextPlan.ForUser(UserId, TenantId), command);

        var guard = command.CommandText[..command.CommandText.LastIndexOf(original, StringComparison.Ordinal)];
        foreach (var word in new[] { "SELECT", "OUTPUT", "INSERT", "UPDATE", "DELETE", "MERGE" })
        {
            guard.ShouldNotContain(word, Case.Insensitive);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void OnlyAPlainTextCommandCanCarryTheContext()
    {
        BatchedSessionContext.CanCarry(new SqlCommand(Select)).ShouldBeTrue();
        BatchedSessionContext.CanCarry(new SqlCommand("dbo.SomeProcedure") { CommandType = CommandType.StoredProcedure }).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AStoredProcedureCommandCannotCarryTheContext()
    {
        // A stored procedure is called by name, so there is no batch to put the context in front of: the command type alone is
        // enough to keep it on the separate apply. (A bulk copy no longer reaches a connection as a command at all, FDW-1152.)
        var procedure = new SqlCommand("dbo.usp_LoadOrders") { CommandType = CommandType.StoredProcedure };

        BatchedSessionContext.CanCarry(procedure).ShouldBeFalse();
    }

    [Theory]
    [InlineData("sp_set_session_context", true)]
    [InlineData("SP_SET_SESSION_CONTEXT", true)]
    [InlineData("sp_executesql", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OnlyTheProcedureThatSetsTheContextMarksAFailureAsTheContexts(string? procedure, bool expected)
    {
        BatchedSessionContext.IsContextProcedure(procedure).ShouldBe(expected);
    }
}
