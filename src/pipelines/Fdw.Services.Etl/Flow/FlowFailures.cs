using System;
using System.Collections.Generic;
using Fdw.Messages;
using Fdw.Results;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A failure of a flow component that keeps its cause (FDW-4197).
/// </summary>
/// <remarks>
/// Why: a run's failure is classified from its first coded failure message (<see cref="FailureCategory"/>, <c>RunFailureClassification</c>), and the HTTP answer reads the
/// same message. A component that ended with a new failure carrying only the cause's text hid what failed: every failed graph run was classified as the flow's own event,
/// whatever the connection or the query had done. Here the cause's messages come first, so the first coded failure message is the cause's, and the flow's own message comes last,
/// so the failure reason every reader prints (the last message) is the same sentence as before.
/// </remarks>
internal static class FlowFailures
{
    /// <summary>The words that say why a failure happened: the first coded failure message, and when it carries none the failure's last message.</summary>
    /// <param name="cause">The failed result.</param>
    /// <returns>The reason. The last message alone is not it: a failed result can end with a message that is not a failure (a failed connection test on dev ended with "Success"), so a sentence built from it said "Success".</returns>
    internal static string? ReasonOf(IGenericResult cause)
    {
        return cause.FirstFailureMessage() is { } coded ? coded.Message.Message : cause.CurrentMessage;
    }

    /// <summary>Builds the failure of a component from its own message and the failure that caused it.</summary>
    /// <param name="context">The flow's message for the component, which says where the failure happened.</param>
    /// <param name="cause">The failed result the component received.</param>
    /// <returns>A failed result whose messages are the cause's, then the context.</returns>
    internal static IGenericResult Caused(IGenericMessage context, IGenericResult cause)
    {
        IReadOnlyList<IGenericMessage> messages = [.. cause.Messages, context];
        var failure = GenericResult.Failure(messages);

        // The cause's details travel with its messages: the connection that raised the failure is still named after the flow adds its sentence (FDW-4279).
        return cause.Details is { } details ? GenericResult.WithDetails(failure, details) : failure;
    }

    /// <summary>Builds the failure of a component that returns a value from its own message and the failure that caused it.</summary>
    /// <typeparam name="T">The type of value the component returns when it succeeds.</typeparam>
    /// <param name="context">The flow's message for the component, which says where the failure happened.</param>
    /// <param name="cause">The failed result the component received.</param>
    /// <returns>A failed result whose messages are the cause's, then the context.</returns>
    internal static IGenericResult<T> Caused<T>(IGenericMessage context, IGenericResult cause) => Caused(context, cause).ToNewResult<T>();
}
