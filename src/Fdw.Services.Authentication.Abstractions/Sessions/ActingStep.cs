using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Abstractions.Sessions;

/// <summary>One session in the chain a person has built by acting with roles (FDW-2282).</summary>
[ExcludeFromCodeCoverage]
public sealed record ActingStep
{
    /// <summary>Gets the step's number: 0 is the person's own session, 1 the first role they acted with, and so on.</summary>
    public required int Step { get; init; }

    /// <summary>Gets the session's <see cref="SessionRecord.Id"/>.</summary>
    public required Guid SessionId { get; init; }

    /// <summary>Gets the role this step acts with, or <see langword="null"/> for the person's own session.</summary>
    public string? Role { get; init; }

    /// <summary>Gets when the session started.</summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>Gets a value indicating whether this is the session the request was made with.</summary>
    public required bool IsCurrent { get; init; }
}
