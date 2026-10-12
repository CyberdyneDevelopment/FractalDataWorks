using System;
using System.Collections.Generic;
using Fdw.Messages;

namespace Fdw.Results;

/// <summary>
/// Extension methods for creating result messages from exception chains.
/// </summary>
public static class ExceptionResultExtensions
{
    /// <summary>
    /// Creates messages from the full exception chain, flattening AggregateExceptions.
    /// </summary>
    /// <param name="ex">The exception to flatten into messages.</param>
    /// <returns>A read-only list of messages representing the full exception chain.</returns>
    public static IReadOnlyList<IGenericMessage> FlattenException(Exception ex)
    {
        if (ex == null)
        {
            throw new ArgumentNullException(nameof(ex));
        }

        var messages = new List<IGenericMessage>();

        if (ex is AggregateException agg)
        {
            foreach (var inner in agg.Flatten().InnerExceptions)
            {
                messages.Add(GenericMessage.Create(
                    MessageSeverity.Error,
                    inner.Message,
                    inner.GetType().Name,
                    inner.Source ?? "Unknown"));
            }
        }
        else
        {
            var current = ex;
            while (current != null)
            {
                messages.Add(GenericMessage.Create(
                    MessageSeverity.Error,
                    current.Message,
                    current.GetType().Name,
                    current.Source ?? "Unknown"));
                current = current.InnerException;
            }
        }

        return messages.AsReadOnly();
    }
}
