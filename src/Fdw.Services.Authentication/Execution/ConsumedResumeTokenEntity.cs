using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Execution;

/// <summary>
/// One row of <c>auth.ConsumedResumeToken</c> (AuthDb): a resume token already spent.
/// Written on every successful consume so a replay is detected as "already consumed" rather than
/// "never existed", even after the originating <see cref="AuthenticationExecutionEntity"/> row is
/// eventually swept.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ConsumedResumeTokenEntity
{
    /// <summary>Gets or sets the unkeyed SHA-256 hash of the resume token. Never the token itself.</summary>
    public byte[] ResumeTokenHash { get; set; } = [];

    /// <summary>Gets or sets when the execution would otherwise have expired. Not read at check time — kept for a future sweep.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
