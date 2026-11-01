using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>
/// Someone who proved who they are, and the terms on which they proved it.
/// </summary>
/// <remarks>
/// <para>
/// The identity key is the pair (<see cref="Issuer"/>, <see cref="SubjectId"/>). A subject
/// identifier is unique only within the issuer that minted it, so neither half identifies anyone on
/// its own. Email is never the key: it is frequently unverified, it changes, and two issuers can
/// assert the same address for different people.
/// </para>
/// <para>
/// Carries the assurance, never the mechanism. Nothing downstream may branch on which provider or
/// protocol produced this — that is what makes one step interchangeable with another.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record Subject
{
    /// <summary>Gets the authority that asserted this subject.</summary>
    public required string Issuer { get; init; }

    /// <summary>Gets the subject identifier, unique within <see cref="Issuer"/>.</summary>
    public required string SubjectId { get; init; }

    /// <summary>Gets when the authentication took place.</summary>
    public required DateTimeOffset AuthenticatedAt { get; init; }
}
