using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Execution;

/// <summary>
/// One row of <c>auth.ConsumedResumeToken</c> (ConfigurationDb): a resume token hash recorded at the
/// moment it was spent, so a replay is still detectable after its execution row is eventually swept.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class ConsumedResumeTokenEntity
{
    /// <summary>Gets or sets the hash of the token that was consumed.</summary>
    public byte[] ResumeTokenHash { get; set; } = [];

    /// <summary>Gets or sets how long this tombstone itself is kept before it can be swept.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
