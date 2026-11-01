using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// The issuers this deployment can resolve, and whether that list is all of them.
/// </summary>
/// <remarks>
/// Completeness is carried rather than assumed because the two answers a caller wants need
/// different amounts of knowledge. "Yes, I can resolve this issuer" needs only that it appears —
/// an authority elsewhere being unreadable cannot make a readable one untrue. "No, nobody can
/// resolve this" needs the whole set, and an incomplete set cannot justify it: the refusal might
/// be naming an authority that is configured and merely unreadable this second.
/// </remarks>
public sealed class ExternalIssuerSet
{
    /// <summary>Initializes a new instance of the <see cref="ExternalIssuerSet"/> class.</summary>
    /// <param name="issuers">The issuers that were read.</param>
    /// <param name="isComplete">Whether every federating step stated its issuer.</param>
    public ExternalIssuerSet(IReadOnlyList<string> issuers, bool isComplete)
    {
        Issuers = issuers ?? throw new ArgumentNullException(nameof(issuers));
        IsComplete = isComplete;
    }

    /// <summary>Gets the issuers that were read.</summary>
    public IReadOnlyList<string> Issuers { get; }

    /// <summary>
    /// Gets whether every federating step stated its issuer. False when one could not be read.
    /// </summary>
    public bool IsComplete { get; }

    /// <summary>
    /// Answers whether the named issuer is one this deployment can resolve.
    /// </summary>
    /// <param name="provider">The issuer a binding names.</param>
    public bool Knows(string provider)
        => Issuers.Any(i => string.Equals(i, provider, StringComparison.OrdinalIgnoreCase));
}
