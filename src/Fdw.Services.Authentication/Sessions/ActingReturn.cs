using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Authentication.Sessions;

/// <summary>
/// The plan for returning from acting (FDW-2282): which sessions end and which one wakes.
/// </summary>
/// <remarks>
/// Kept apart from the store so the rule can be tested without a database. The chain is read from the session the request was made with up to
/// the person's own session; a step counts from the other end, so step 0 is always the person's own session and the same step names the same
/// session however deep the chain grows.
/// </remarks>
internal static class ActingReturn
{
    /// <summary>The most sessions a chain of acting may hold, the person's own session included.</summary>
    internal const int MaxDepth = 32;

    /// <summary>Whether a chain of this length may be extended by one more acting session.</summary>
    /// <param name="chainLength">The sessions in the chain now, the one acting from included.</param>
    /// <returns><see langword="true"/> when the extended chain stays within <see cref="MaxDepth"/>.</returns>
    internal static bool MayDeepen(int chainLength) => chainLength < MaxDepth;

    /// <summary>What returning to a step does.</summary>
    /// <param name="Revoke">The sessions to end, top first.</param>
    /// <param name="Wake">The session to wake.</param>
    internal sealed record Plan(IReadOnlyList<Guid> Revoke, Guid Wake);

    /// <summary>Plans a return.</summary>
    /// <param name="chain">The session ids from the current session up to the person's own, current first.</param>
    /// <param name="step">The step to return to.</param>
    /// <param name="refusal">Why the return is refused; null when it is planned.</param>
    /// <returns>The plan, or null when refused.</returns>
    internal static Plan? Of(IReadOnlyList<Guid> chain, int step, out string? refusal)
    {
        ArgumentNullException.ThrowIfNull(chain);

        if (chain.Count < 2)
        {
            refusal = "the session is not acting";
            return null;
        }

        // The session in hand is the last step; a return goes to one before it.
        if (step < 0 || step > chain.Count - 2)
        {
            refusal = $"there is no step {step} below the session in hand; the steps are 0 to {chain.Count - 2}";
            return null;
        }

        var target = chain.Count - 1 - step;
        refusal = null;
        return new Plan([.. chain.Take(target)], chain[target]);
    }
}
