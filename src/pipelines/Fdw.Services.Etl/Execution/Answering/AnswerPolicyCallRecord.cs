using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution.Answering;

/// <summary>A row of <c>pipe.AnswerPolicyCall</c> (FDW-6142), as the configuration gateway reads it: what one call requires under a policy.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class AnswerPolicyCallRecord
{
    /// <summary>Gets or sets the row's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the policy the row belongs to.</summary>
    public Guid PolicyId { get; set; }

    /// <summary>Gets or sets the call's name.</summary>
    public string CallName { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the requirement the policy states for the call.</summary>
    public string RequirementName { get; set; } = string.Empty;

    /// <summary>Gets or sets the permission a stored grant must allow to cover the call; null for a requirement that states none.</summary>
    public string? Permission { get; set; }

    /// <summary>Gets or sets a value indicating whether this is the current version of the row.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the row is retired.</summary>
    public bool IsDeleted { get; set; }
}
