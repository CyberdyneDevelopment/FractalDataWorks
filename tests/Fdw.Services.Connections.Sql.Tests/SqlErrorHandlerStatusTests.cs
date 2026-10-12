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
}
