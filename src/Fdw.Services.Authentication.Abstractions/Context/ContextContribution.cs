using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>
/// What a step produced. Not a context.
/// </summary>
/// <remarks>
/// A step returns its contribution rather than a whole context so that it cannot rewrite what it did
/// not produce. The runner merges, filtered to what the step declared it contributes, and an element
/// outside that declaration is discarded and reported — never silently accepted, because a
/// declaration nothing checks is a comment.
/// <para>
/// There is no path here to the achieved methods or assurance level. A step reports what it found;
/// only the runner records what was proved.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record ContextContribution
{
    /// <summary>Gets the subject this step proved, if it proved one.</summary>
    public Subject? Subject { get; init; }

    /// <summary>Gets the principal this step resolved, if it resolved one.</summary>
    public Principal? Principal { get; init; }

    /// <summary>Gets the claims this step gathered.</summary>
    public IReadOnlyList<Claim> Claims { get; init; } = [];

    /// <summary>Gets the decision this step reached, if it reached one.</summary>
    public Decision? Decision { get; init; }

    /// <summary>Gets the RFC 8176 methods this step observed being proved.</summary>
    /// <remarks>
    /// Reported, not recorded. The runner keeps only those the step also declared it may assert, so
    /// this cannot raise assurance beyond what configuration already permits — a step that reports
    /// <c>hwk</c> while declaring only <c>pwd</c> records nothing extra.
    /// <para>
    /// Empty means "whatever the step declares it proves", which is the ordinary case for a step
    /// that performs the check itself rather than relaying someone else's.
    /// </para>
    /// </remarks>
    public IReadOnlyList<string> ObservedMethods { get; init; } = [];

    /// <summary>Enumerates the elements actually present, for checking against the declaration.</summary>
    public IEnumerable<IContextElement> Present()
    {
        if (Subject is not null) yield return ContextElements.Subject;
        if (Principal is not null) yield return ContextElements.Principal;
        if (Claims.Count > 0) yield return ContextElements.Claims;
        if (Decision is not null) yield return ContextElements.Decision;
    }
}
