using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Execution;

/// <summary>
/// The single column an atomic consume touches on <c>auth.AuthenticationExecution</c>. Narrower than
/// <see cref="AuthenticationExecutionEntity"/> on purpose — an update built from this type only ever
/// sets <see cref="ConsumedAt"/>, so the WHERE clause guarding the consume is what makes it atomic and
/// nothing else on the row is at risk of being overwritten with stale values.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AuthenticationExecutionConsumeUpdate
{
    /// <summary>Gets or sets when this execution was consumed.</summary>
    public DateTimeOffset ConsumedAt { get; set; }
}
