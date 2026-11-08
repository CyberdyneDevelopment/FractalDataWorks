using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.MsSql.ErrorHandlers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Sql.Tests;

/// <summary>
/// A SQL error answers with the status of its own kind. Before FDW-1180 every error without a handler fell to the
/// unhandled sentinel, whose number sits in the Dependency category: a NOT NULL violation from the application's own
/// insert reached the caller as a retryable 502 "downstream service unavailable", and the edge replaced that body.
/// </summary>
public sealed class SqlErrorHandlerStatusTests
{
    private static int StatusOf(int sqlErrorNumber)
    {
        var handler = SqlErrorHandlers.ByErrorNumber(sqlErrorNumber);
        var message = handler.CreateFailureMessage(NullLogger.Instance, new InvalidOperationException("the server's words"), "INSERT INTO t");
        return ResultCategories.ById(NumberOf(message) / 10000).HttpStatus;
    }

    private static int NumberOf(IGenericMessage message)
    {
        var code = message.Code.ShouldNotBeNull();
        return int.Parse(code[(code.LastIndexOf('-') + 1)..], NumberStyles.None, CultureInfo.InvariantCulture);
    }

    [Theory]
    [InlineData(2601, 409)]
    [InlineData(2627, 409)]
    [InlineData(547, 409)]
    [InlineData(33504, 403)]
    [InlineData(515, 500)]
    [InlineData(8003, 500)]
    public void A_constraint_or_statement_error_answers_with_its_own_status(int sqlErrorNumber, int expectedStatus)
        => StatusOf(sqlErrorNumber).ShouldBe(expectedStatus);

    [Fact]
    public void A_duplicate_key_is_the_catalogs_already_exists_number()
    {
        var message = SqlErrorHandlers.ByErrorNumber(2627)
            .CreateFailureMessage(NullLogger.Instance, new InvalidOperationException("x"), "INSERT INTO t");

        NumberOf(message).ShouldBe(40001);
    }

    [Fact]
    public void An_error_nobody_wrote_a_handler_for_still_answers_as_the_dependency_failure()
        => StatusOf(99999).ShouldBe(502);

    [Theory]
    [InlineData(2601)]
    [InlineData(2627)]
    [InlineData(547)]
    [InlineData(515)]
    [InlineData(8003)]
    [InlineData(33504)]
    public void Sending_the_same_statement_again_fails_the_same_way(int sqlErrorNumber)
        => SqlErrorHandlers.ByErrorNumber(sqlErrorNumber).IsRetryable.ShouldBeFalse();

    [Fact]
    public void The_server_message_is_carried_for_a_statement_the_application_built_wrongly()
    {
        var message = SqlErrorHandlers.ByErrorNumber(515)
            .CreateFailureMessage(NullLogger.Instance, new InvalidOperationException("Cannot insert NULL into 'IsEnabled'"), "INSERT INTO t");

        message.Message.ShouldContain("IsEnabled");
        message.Message.ShouldContain("515");
    }

    [Fact]
    public void No_two_handlers_claim_the_same_error_number()
    {
        // Why: ByErrorNumber keeps the first handler for a number and drops the rest without a word.
        var claimed = SqlErrorHandlers.All().SelectMany(handler => handler.SqlErrorNumbers).ToList();

        claimed.Count.ShouldBe(claimed.Distinct().Count());
    }

    [Theory]
    [InlineData(8134, 16)]
    [InlineData(102, 15)]
    [InlineData(245, 16)]
    [InlineData(50000, 16)]
    [InlineData(8115, 16)]
    public void A_statement_the_server_rejects_on_a_working_connection_is_a_validation_failure_that_is_not_retried(int sqlErrorNumber, int sqlErrorClass)
    {
        // Why (FDW-4361): a divide by zero in a script had no handler and fell to the unhandled sentinel, a retryable Dependency failure, so a run that stopped
        // on it was read as an unreachable connection and never counted toward a failure threshold.
        var handler = SqlErrorHandlers.ByError(sqlErrorNumber, sqlErrorClass);
        var message = handler.CreateFailureMessage(NullLogger.Instance, new InvalidOperationException("Divide by zero error encountered."), "SELECT 1/0");

        NumberOf(message).ShouldBe(22007);
        ResultCategories.ById(NumberOf(message) / 10000).HttpStatus.ShouldBe(400);
        handler.IsRetryable.ShouldBeFalse();
        message.Message.ShouldContain("Divide by zero");
    }

    [Theory]
    [InlineData(208, 16, 30000)]
    [InlineData(229, 14, 50001)]
    [InlineData(2627, 14, 40001)]
    [InlineData(547, 16, 40000)]
    [InlineData(18456, 14, 51004)]
    [InlineData(4060, 11, 71022)]
    [InlineData(1205, 13, 81000)]
    public void A_number_with_its_own_handler_keeps_it_whatever_its_class(int sqlErrorNumber, int sqlErrorClass, int expectedCode)
    {
        var message = SqlErrorHandlers.ByError(sqlErrorNumber, sqlErrorClass)
            .CreateFailureMessage(NullLogger.Instance, new InvalidOperationException("x"), "SELECT 1");

        NumberOf(message).ShouldBe(expectedCode);
    }

    [Theory]
    [InlineData(1222, 16)]
    [InlineData(-2, 11)]
    public void A_lock_or_query_timeout_is_still_retried_and_is_not_taken_for_a_statements_own_error(int sqlErrorNumber, int sqlErrorClass)
        => SqlErrorHandlers.ByError(sqlErrorNumber, sqlErrorClass).IsRetryable.ShouldBeTrue();

    [Theory]
    [InlineData(17)]
    [InlineData(19)]
    [InlineData(20)]
    [InlineData(25)]
    [InlineData(10)]
    public void A_class_no_handler_claims_still_answers_as_the_unhandled_dependency_failure(int sqlErrorClass)
    {
        var handler = SqlErrorHandlers.ByError(99999, sqlErrorClass);
        var message = handler.CreateFailureMessage(NullLogger.Instance, new InvalidOperationException("x"), "SELECT 1");

        NumberOf(message).ShouldBe(71017);
        ResultCategories.ById(NumberOf(message) / 10000).HttpStatus.ShouldBe(502);
    }

    [Fact]
    public void No_two_handlers_claim_the_same_severity_class()
    {
        var claimed = SqlErrorHandlers.All().SelectMany(handler => handler.SqlErrorClasses).ToList();

        claimed.Count.ShouldBe(claimed.Distinct().Count());
    }

    [Fact]
    public void A_write_refused_by_row_level_security_is_forbidden_and_does_not_carry_the_server_text()
    {
        var message = SqlErrorHandlers.ByErrorNumber(33504)
            .CreateFailureMessage(NullLogger.Instance, new InvalidOperationException("The attempted operation failed because the target object 'devConfigurationDb.conn.ConnectionDisabled' has a block predicate"), "INSERT INTO t");

        FailureCategory.IsForbidden(GenericResult.Failure(message)).ShouldBeTrue();
        message.Message.ShouldNotContain("devConfigurationDb");
        message.Message.ShouldNotContain("block predicate");
    }
}
