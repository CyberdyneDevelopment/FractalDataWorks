using System;
using System.Collections.Generic;
using Fdw.Messages;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// The failure of a connection the connection provider would not give (FDW-4211): the provider's own messages first, the data layer's sentence last.
/// </summary>
/// <remarks>
/// A run's failure is classified by its first coded failure message and the HTTP answer reads the same message, so a connection the provider refused (a disabled one answers a
/// Refused code, FDW-3951) must keep that code in front; a failure built from the data layer's wrapper alone was classified by the wrapper (Missing), and the failure rule counted
/// a pipeline for a connection nobody could use. The sentence every reader prints, the last message, stays the data layer's.
/// </remarks>
public static class ConnectionFailures
{
    /// <summary>The key of the failure's details that holds the Id of the connection that raised it (FDW-4279).</summary>
    public const string ConnectionIdKey = "connectionId";

    /// <summary>Builds the failure of a connection lookup from the provider's failure and the data layer's own message.</summary>
    /// <typeparam name="T">The type of value the lookup's caller returns when it succeeds.</typeparam>
    /// <param name="cause">The failed result the connection provider returned.</param>
    /// <param name="context">The data layer's message, which says which data store or connection was being resolved.</param>
    /// <returns>A failed result whose messages are the cause's, then the context.</returns>
    public static IGenericResult<T> Caused<T>(IGenericResult cause, IGenericMessage context)
    {
        if (cause is null)
        {
            throw new System.ArgumentNullException(nameof(cause));
        }

        if (context is null)
        {
            throw new System.ArgumentNullException(nameof(context));
        }

        var messages = new List<IGenericMessage>(cause.Messages) { context };
        var failure = GenericResult.Failure(messages);

        // The cause's details travel with its messages: a connection that raised the failure is still named after the data layer adds its sentence (FDW-4279).
        return cause.Details is { } details
            ? GenericResult<T>.WithDetails(failure, details)
            : failure.ToNewResult<T>();
    }

    /// <summary>States that a connection raised a failure (FDW-4279).</summary>
    /// <typeparam name="T">The type of value the operation returns when it succeeds.</typeparam>
    /// <param name="failure">The failed result the connection returned.</param>
    /// <param name="connectionId">The Id of the connection that raised it.</param>
    /// <returns>The failure with its Code, messages and chain unchanged and the connection named in its details.</returns>
    public static IGenericResult<T> RaisedBy<T>(IGenericResult failure, Guid connectionId)
    {
        if (failure is null)
        {
            throw new ArgumentNullException(nameof(failure));
        }

        return GenericResult<T>.WithDetails(failure, ResultDetails.Create(ConnectionIdKey, connectionId.ToString("D")));
    }

    /// <summary>Reads the connection a failure names as its raiser (FDW-4279).</summary>
    /// <param name="failure">The failed result.</param>
    /// <returns>The Id of the connection that raised it; null when the failure names none, which means a connection did not raise it.</returns>
    public static Guid? ConnectionOf(IGenericResult failure)
    {
        if (failure is null)
        {
            throw new ArgumentNullException(nameof(failure));
        }

        for (var link = failure; link is not null; link = link.InnerResult)
        {
            if (link.Details is { } details
                && details.Data.TryGetValue(ConnectionIdKey, out var named)
                && named is string text
                && Guid.TryParse(text, out var id))
            {
                return id;
            }
        }

        return null;
    }
}
