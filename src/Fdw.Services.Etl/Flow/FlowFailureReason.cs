using System.Linq;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// The reason a script step gives for a failed gateway call (FDW-3302): the first failure-category message, which is the one the API
/// answers with too.
/// </summary>
internal static class FlowFailureReason
{
    /// <summary>
    /// Gets the reason for <paramref name="failed"/>. A failure that carried no coded failure message is itself a defect upstream, so its
    /// reason says that and lists the messages it did carry, and a MessageLogging event records it; the reason is never left empty and
    /// never borrows the last message's text.
    /// </summary>
    /// <param name="failed">The failed result of the gateway call.</param>
    /// <param name="logger">The logger of the step.</param>
    /// <param name="step">The name of the step or component.</param>
    /// <param name="dataStore">The name of the data store the script ran on.</param>
    /// <returns>The reason.</returns>
    internal static string Of(IGenericResult failed, ILogger logger, string step, string dataStore)
    {
        if (failed.FirstFailureMessage() is { } coded)
        {
            return coded.Message.Message;
        }

        var carried = failed.Messages.Count == 0
            ? "no messages"
            : string.Join("; ", failed.Messages.Select(message => string.Concat(message.Code is { Length: > 0 } code ? code : "no code", ": ", message.Message)));
        FlowScriptLog.FailureCarriedNoCodedMessage(logger, step, dataStore, carried);
        return string.Concat("the failure carried no coded failure message; it carried ", carried);
    }
}
