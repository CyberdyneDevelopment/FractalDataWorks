using System;
using Fdw.Protocols.Tds;
using Fdw.Results;
using Fdw.Tds.Endpoints.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Tds.Endpoints.Sql;

/// <summary>
/// Raises and recognises a refusal whose answer to the client is a SQL Server error.
/// </summary>
/// <remarks>
/// A refusal is an ordinary failed result, so it propagates with <c>ToNewResult</c> like any other.
/// The batch runner asks <see cref="TryGetMessage"/> which error to send: a refusal answers its own,
/// and any other failure is the data platform's and is sent as error 50000 with its code.
/// </remarks>
public static class SqlRefusal
{
    /// <summary>The details key the SQL Server error travels under.</summary>
    public const string MessageKey = "sqlMessage";

    /// <summary>Refuses with a SQL Server error.</summary>
    /// <typeparam name="T">The result's value type.</typeparam>
    /// <param name="message">The error the client receives.</param>
    /// <param name="logger">The logger the refusal is recorded through.</param>
    /// <returns>The failed result.</returns>
    public static IGenericResult<T> Refuse<T>(SqlServerMessage message, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(message);
        return GenericResult<T>.Failure(
            TdsEndpointResultCodes.ByName("StatementRefused"), logger,
            ResultDetails.Create(MessageKey, message, "number", message.Number, "text", message.Text));
    }

    /// <summary>Refuses with a SQL Server error, for an operation with no value.</summary>
    /// <param name="message">The error the client receives.</param>
    /// <param name="logger">The logger the refusal is recorded through.</param>
    /// <returns>The failed result.</returns>
    public static IGenericResult Refuse(SqlServerMessage message, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(message);
        return GenericResult.Failure(
            TdsEndpointResultCodes.ByName("StatementRefused"), logger,
            ResultDetails.Create(MessageKey, message, "number", message.Number, "text", message.Text));
    }

    /// <summary>Finds the SQL Server error a failure carries, looking through the results it wraps.</summary>
    /// <param name="failure">The failed result.</param>
    /// <param name="message">The error, when the failure is a refusal.</param>
    /// <returns>True when the failure is a refusal.</returns>
    public static bool TryGetMessage(IGenericResult failure, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out SqlServerMessage? message)
    {
        for (var result = failure; result is not null; result = result.InnerResult)
        {
            if (result.Details?.GetValue<SqlServerMessage>(MessageKey) is { } carried)
            {
                message = carried;
                return true;
            }
        }

        message = null;
        return false;
    }
}
