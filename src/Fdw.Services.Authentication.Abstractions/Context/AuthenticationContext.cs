using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>
/// What the flow has established so far.
/// </summary>
/// <remarks>
/// <see cref="AchievedMethods"/> and <see cref="AchievedAcr"/> have no public setter by design. A
/// step that could name its own authentication method could claim a factor it never checked, and
/// every downstream test of assurance — step-up, high-value operations, audit — would then be
/// unfalsifiable. The runner records what was proved, from each step's declared method, once that
/// step has actually succeeded.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record AuthenticationContext
{
    /// <summary>Gets the subject, once some step has proved one.</summary>
    public Subject? Subject { get; init; }

    /// <summary>Gets the local principal, once some step has resolved one.</summary>
    public Principal? Principal { get; init; }

    /// <summary>Gets the claims gathered so far.</summary>
    public ClaimSet Claims { get; init; } = ClaimSet.Empty;

    /// <summary>Gets the issuance decision, once some step has reached one.</summary>
    public Decision? Decision { get; init; }

    /// <summary>Gets the authentication methods proved so far — RFC 8176 values.</summary>
    public ImmutableArray<string> AchievedMethods { get; internal init; } = [];

    /// <summary>Gets the assurance level the achieved methods amount to.</summary>
    public string? AchievedAcr { get; internal init; }

    /// <summary>Returns whether every element in <paramref name="required"/> is present.</summary>
    /// <param name="required">The elements a step declared it requires.</param>
    public bool Satisfies(IEnumerable<IContextElement> required)
        => required.All(Has);

    /// <summary>Returns whether <paramref name="element"/> is present.</summary>
    /// <param name="element">The element to test for.</param>
    public bool Has(IContextElement element) => element.IsPresentOn(this);
}
