using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Authentication.Execution;

/// <summary>
/// One row of <c>auth.AuthenticationExecution</c> (AuthDb): a login suspended
/// part-way, waiting for its caller to come back. <see cref="ContextData"/> is the encrypted output of
/// an <c>EncryptCommand</c> run by <c>IEncryptionCommandExecutor</c> — never plaintext.
/// </summary>
/// <remarks>
/// Read back through <c>Query.From&lt;AuthenticationExecutionEntity&gt;</c> in <c>DatabaseExecutionStore</c>, which needs a generated
/// mapper; without <c>[GenerateMapper]</c> the insert succeeds and the read fails with "No POCO mapper
/// found" (FDW-918, the same defect SessionEntity had).
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class AuthenticationExecutionEntity
{
    /// <summary>Gets or sets the execution's durable identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the flow being run.</summary>
    public string FlowName { get; set; } = string.Empty;

    /// <summary>Gets or sets the unkeyed SHA-256 hash of the resume token. Never the token itself.</summary>
    public byte[] ResumeTokenHash { get; set; } = [];

    /// <summary>Gets or sets the encrypted, serialized <c>AuthenticationContext</c>.</summary>
    public byte[] ContextData { get; set; } = [];

    /// <summary>Gets or sets the index of the step that suspended.</summary>
    public int CurrentStepIndex { get; set; }

    /// <summary>Gets or sets when this execution stops being resumable.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Gets or sets when this execution was consumed. Null until then.</summary>
    public DateTimeOffset? ConsumedAt { get; set; }
}
