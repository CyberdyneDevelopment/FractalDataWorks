using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Execution;

/// <summary>
/// One row of <c>auth.AuthenticationExecution</c> (ConfigurationDb): a login suspended part-way,
/// waiting for its caller to come back from an identity provider.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class ExecutionRecordEntity
{
    /// <summary>Gets or sets the execution's logical identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the flow being run.</summary>
    public string FlowName { get; set; } = string.Empty;

    /// <summary>Gets or sets the hash of the resume token — never the token itself.</summary>
    public byte[] ResumeTokenHash { get; set; } = [];

    /// <summary>Gets or sets the encrypted, serialized <c>AuthenticationContext</c>.</summary>
    public byte[] ContextData { get; set; } = [];

    /// <summary>Gets or sets the index of the step that suspended.</summary>
    public int CurrentStepIndex { get; set; }

    /// <summary>Gets or sets when this execution stops being resumable.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Gets or sets when this execution was consumed, or <see langword="null"/> if it has not been.</summary>
    public DateTimeOffset? ConsumedAt { get; set; }
}
